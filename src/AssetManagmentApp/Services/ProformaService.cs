using AssetManagmentApp.Data;
using AssetManagmentApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagmentApp.Services;

public class ProformaService(AppDbContext db)
{
    public async Task<List<ProformaLineaPreview>> CalcularDetalleAsync(int proyectoId, DateOnly fechaCorte)
    {
        var asignaciones = await db.AsignacionesActivoProyecto
            .Include(a => a.Activo).ThenInclude(a => a.TipoEquipo)
            .Where(a => a.ProyectoId == proyectoId
                && a.FechaUltimoCobro < fechaCorte
                && (a.FechaSalida == null || a.FechaUltimoCobro < a.FechaSalida))
            .ToListAsync();

        if (asignaciones.Count == 0)
        {
            return [];
        }

        var tipoEquipoIds = asignaciones.Select(a => a.Activo.TipoEquipoId).Distinct().ToList();
        var historialesPorTipo = (await db.HistorialPreciosTipoEquipo
                .Where(h => tipoEquipoIds.Contains(h.TipoEquipoId))
                .ToListAsync())
            .GroupBy(h => h.TipoEquipoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.VigenteDesde).ToList());

        var lineas = new List<ProformaLineaPreview>();

        foreach (var asignacion in asignaciones)
        {
            var hastaExclusive = asignacion.FechaSalida is not null && asignacion.FechaSalida.Value < fechaCorte
                ? asignacion.FechaSalida.Value
                : fechaCorte;

            if (hastaExclusive <= asignacion.FechaUltimoCobro)
            {
                continue;
            }

            var historial = historialesPorTipo.TryGetValue(asignacion.Activo.TipoEquipoId, out var lista)
                ? lista
                : [];

            foreach (var tramo in PartirPorCambioDePrecio(asignacion.FechaUltimoCobro, hastaExclusive, historial))
            {
                var dias = tramo.HastaExclusive.DayNumber - tramo.Desde.DayNumber;
                var subtotal = Math.Round(tramo.Precio * dias, 2);

                lineas.Add(new ProformaLineaPreview
                {
                    AsignacionId = asignacion.Id,
                    ActivoId = asignacion.ActivoId,
                    Placa = asignacion.Activo.Placa,
                    TipoEquipoNombre = asignacion.Activo.TipoEquipo.Nombre,
                    PrecioPorDiaUsado = tramo.Precio,
                    DiasCobrados = dias,
                    Subtotal = subtotal,
                    PeriodoDesde = tramo.Desde,
                    PeriodoHastaExclusive = tramo.HastaExclusive,
                    NuevaFechaUltimoCobro = hastaExclusive
                });
            }
        }

        return lineas;
    }

    public async Task<Proforma> GenerarAsync(int proyectoId, DateOnly fechaCorte, int usuarioId)
    {
        var lineas = await CalcularDetalleAsync(proyectoId, fechaCorte);
        if (lineas.Count == 0)
        {
            throw new InvalidOperationException("No hay días pendientes de facturar para este proyecto.");
        }

        var fechaGeneracion = DateTime.Now;

        var proforma = new Proforma
        {
            Numero = await GenerarNumeroConsecutivoAsync(fechaGeneracion),
            ProyectoId = proyectoId,
            FechaGeneracion = DateOnly.FromDateTime(fechaGeneracion),
            PeriodoDesde = lineas.Min(l => l.PeriodoDesde),
            PeriodoHasta = fechaCorte,
            Total = lineas.Sum(l => l.Subtotal),
            UsuarioGeneroId = usuarioId,
            EnviadaPorCorreo = false,
            Estado = EstadoProforma.Generada
        };

        foreach (var linea in lineas)
        {
            proforma.Detalles.Add(new ProformaDetalle
            {
                ActivoId = linea.ActivoId,
                TipoEquipoNombre = linea.TipoEquipoNombre,
                PrecioPorDiaUsado = linea.PrecioPorDiaUsado,
                DiasCobrados = linea.DiasCobrados,
                Subtotal = linea.Subtotal
            });
        }

        db.Proformas.Add(proforma);

        var nuevaFechaPorAsignacionId = lineas
            .GroupBy(l => l.AsignacionId)
            .ToDictionary(g => g.Key, g => g.First().NuevaFechaUltimoCobro);

        var asignacionIds = nuevaFechaPorAsignacionId.Keys.ToList();
        var asignaciones = await db.AsignacionesActivoProyecto
            .Where(a => asignacionIds.Contains(a.Id))
            .ToListAsync();

        foreach (var asignacion in asignaciones)
        {
            asignacion.FechaUltimoCobro = nuevaFechaPorAsignacionId[asignacion.Id];
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return proforma;
    }

    public async Task AnularAsync(int proformaId)
    {
        var proforma = await db.Proformas.FirstOrDefaultAsync(p => p.Id == proformaId)
            ?? throw new InvalidOperationException("La proforma no existe.");

        proforma.Estado = EstadoProforma.Anulada;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private async Task<string> GenerarNumeroConsecutivoAsync(DateTime fechaGeneracion)
    {
        var prefijo = fechaGeneracion.ToString("yyyyMM");

        var ultimoNumero = await db.Proformas
            .Where(p => p.Numero.StartsWith(prefijo))
            .OrderByDescending(p => p.Numero)
            .Select(p => p.Numero)
            .FirstOrDefaultAsync();

        var siguienteConsecutivo = 1;
        if (ultimoNumero is not null && ultimoNumero.Length == prefijo.Length + 3
            && int.TryParse(ultimoNumero[prefijo.Length..], out var consecutivo))
        {
            siguienteConsecutivo = consecutivo + 1;
        }

        return $"{prefijo}{siguienteConsecutivo:D3}";
    }

    // HistorialPrecioTipoEquipo es no-solapado y sin huecos (ver TiposEquipo/Index.razor),
    // así que basta recorrerlo en orden e intersecar con [desde, hastaExclusive).
    private static IEnumerable<(DateOnly Desde, DateOnly HastaExclusive, decimal Precio)> PartirPorCambioDePrecio(
        DateOnly desde, DateOnly hastaExclusive, List<HistorialPrecioTipoEquipo> historial)
    {
        for (var i = 0; i < historial.Count; i++)
        {
            var h = historial[i];

            // El primer precio registrado se extiende hacia atrás si la asignación
            // es anterior a cuando se registró ese precio, ya que no hay un precio
            // previo con el cual facturar esos días (evita perder días facturables).
            var tramoDesde = i == 0 ? desde : (h.VigenteDesde > desde ? h.VigenteDesde : desde);
            var vigenteHastaExclusive = h.VigenteHasta.HasValue ? h.VigenteHasta.Value.AddDays(1) : hastaExclusive;
            var tramoHasta = vigenteHastaExclusive < hastaExclusive ? vigenteHastaExclusive : hastaExclusive;

            if (tramoHasta > tramoDesde)
            {
                yield return (tramoDesde, tramoHasta, h.Precio);
            }
        }
    }
}
