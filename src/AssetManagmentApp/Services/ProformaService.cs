using AssetManagmentApp.Data;
using AssetManagmentApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagmentApp.Services;

public class ProformaService(AppDbContext db, TipoCambioService tipoCambioService)
{
    public async Task<List<ProformaLineaPreview>> CalcularDetalleAsync(int proyectoId, DateOnly fechaCorte)
    {
        var asignaciones = await db.AsignacionesActivoProyecto
            .AsNoTracking()
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
                .AsNoTracking()
                .Where(h => tipoEquipoIds.Contains(h.TipoEquipoId))
                .ToListAsync())
            .GroupBy(h => h.TipoEquipoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.VigenteDesde).ToList());

        var activoIds = asignaciones.Select(a => a.ActivoId).Distinct().ToList();
        var cambiosEstadoPorActivo = await ObtenerCambiosEstadoPorActivoAsync(activoIds);

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

            var intervalosDanado = ObtenerIntervalosDanado(
                cambiosEstadoPorActivo.TryGetValue(asignacion.ActivoId, out var cambios) ? cambios : [],
                hastaExclusive);
            var rangosFacturables = RestarIntervalos(asignacion.FechaUltimoCobro, hastaExclusive, intervalosDanado);

            foreach (var rango in rangosFacturables)
            {
                foreach (var tramo in PartirPorCambioDePrecio(rango.Desde, rango.HastaExclusive, historial))
                {
                    var dias = tramo.HastaExclusive.DayNumber - tramo.Desde.DayNumber;
                    var subtotal = Math.Round(tramo.Precio * dias, 2);

                    lineas.Add(new ProformaLineaPreview
                    {
                        AsignacionId = asignacion.Id,
                        ActivoId = asignacion.ActivoId,
                        Placa = asignacion.Activo.Placa,
                        TipoEquipoNombre = asignacion.Activo.TipoEquipo.Nombre,
                        CodigoAlquiler = asignacion.Activo.TipoEquipo.CodigoAlquiler,
                        PrecioPorDiaUsado = tramo.Precio,
                        DiasCobrados = dias,
                        Subtotal = subtotal,
                        PeriodoDesde = tramo.Desde,
                        PeriodoHastaExclusive = tramo.HastaExclusive,
                        NuevaFechaUltimoCobro = hastaExclusive
                    });
                }
            }
        }

        return lineas;
    }

    // Consumo pendiente de facturar por proyecto: lo acumulado desde la FechaUltimoCobro
    // de cada asignación (es decir, desde el último corte / proforma generada) hasta
    // fechaCorte. No usa las proformas ya generadas, ya que esas quedan "en cero" tras
    // el corte (ver GenerarAsync, que avanza FechaUltimoCobro).
    public async Task<List<ConsumoPorProyecto>> CalcularConsumoPorProyectoAsync(DateOnly fechaCorte, int? proyectoId = null)
    {
        var asignacionesQuery = db.AsignacionesActivoProyecto
            .AsNoTracking()
            .Include(a => a.Activo).ThenInclude(a => a.TipoEquipo)
            .Include(a => a.Proyecto)
            .Where(a => a.FechaUltimoCobro < fechaCorte
                && (a.FechaSalida == null || a.FechaUltimoCobro < a.FechaSalida));

        if (proyectoId is not null)
        {
            asignacionesQuery = asignacionesQuery.Where(a => a.ProyectoId == proyectoId);
        }

        var asignaciones = await asignacionesQuery.ToListAsync();

        if (asignaciones.Count == 0)
        {
            return [];
        }

        var tipoEquipoIds = asignaciones.Select(a => a.Activo.TipoEquipoId).Distinct().ToList();
        var historialesPorTipo = (await db.HistorialPreciosTipoEquipo
                .AsNoTracking()
                .Where(h => tipoEquipoIds.Contains(h.TipoEquipoId))
                .ToListAsync())
            .GroupBy(h => h.TipoEquipoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.VigenteDesde).ToList());

        var activoIds = asignaciones.Select(a => a.ActivoId).Distinct().ToList();
        var cambiosEstadoPorActivo = await ObtenerCambiosEstadoPorActivoAsync(activoIds);

        var totalesPorProyecto = new Dictionary<int, (string Nombre, decimal Total)>();

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

            var intervalosDanado = ObtenerIntervalosDanado(
                cambiosEstadoPorActivo.TryGetValue(asignacion.ActivoId, out var cambios) ? cambios : [],
                hastaExclusive);
            var rangosFacturables = RestarIntervalos(asignacion.FechaUltimoCobro, hastaExclusive, intervalosDanado);

            decimal subtotalAsignacion = 0;
            foreach (var rango in rangosFacturables)
            {
                foreach (var tramo in PartirPorCambioDePrecio(rango.Desde, rango.HastaExclusive, historial))
                {
                    var dias = tramo.HastaExclusive.DayNumber - tramo.Desde.DayNumber;
                    subtotalAsignacion += Math.Round(tramo.Precio * dias, 2);
                }
            }

            if (subtotalAsignacion == 0)
            {
                continue;
            }

            totalesPorProyecto[asignacion.ProyectoId] = totalesPorProyecto.TryGetValue(asignacion.ProyectoId, out var actual)
                ? (actual.Nombre, actual.Total + subtotalAsignacion)
                : (asignacion.Proyecto.Nombre, subtotalAsignacion);
        }

        return totalesPorProyecto
            .Select(kv => new ConsumoPorProyecto(kv.Key, kv.Value.Nombre, kv.Value.Total))
            .OrderByDescending(c => c.Total)
            .ToList();
    }

    public async Task<Proforma> GenerarAsync(int proyectoId, DateOnly fechaCorte, int usuarioId)
    {
        var lineas = await CalcularDetalleAsync(proyectoId, fechaCorte);
        if (lineas.Count == 0)
        {
            throw new InvalidOperationException("No hay días pendientes de facturar para este proyecto.");
        }

        var fechaGeneracion = DateTime.Now;
        var tipoCambio = await tipoCambioService.ObtenerVentaDelDiaAsync();

        var proforma = new Proforma
        {
            Numero = await GenerarNumeroConsecutivoAsync(fechaGeneracion),
            ProyectoId = proyectoId,
            FechaGeneracion = DateOnly.FromDateTime(fechaGeneracion),
            PeriodoDesde = lineas.Min(l => l.PeriodoDesde),
            PeriodoHasta = fechaCorte,
            Total = lineas.Sum(l => l.Subtotal),
            TipoCambio = tipoCambio,
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
                CodigoAlquiler = linea.CodigoAlquiler,
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
        var proforma = await db.Proformas.Include(p => p.Detalles).FirstOrDefaultAsync(p => p.Id == proformaId)
            ?? throw new InvalidOperationException("La proforma no existe.");

        proforma.Estado = EstadoProforma.Anulada;

        // Devuelve los días facturados por esta proforma: si el activo no tiene NINGUNA
        // otra proforma (ni generada ni anulada), es seguro asumir que su FechaUltimoCobro
        // venía de FechaIngreso (invariante "sin facturar"), así que se revierte ahí. Si
        // hay otra proforma de por medio no se toca, para no perder días de otro corte.
        var activoIds = proforma.Detalles.Select(d => d.ActivoId).Distinct().ToList();
        var asignaciones = await db.AsignacionesActivoProyecto
            .Where(a => a.ProyectoId == proforma.ProyectoId && activoIds.Contains(a.ActivoId))
            .ToListAsync();

        foreach (var asignacion in asignaciones)
        {
            var tieneOtraProforma = await db.ProformaDetalles
                .AnyAsync(d => d.ActivoId == asignacion.ActivoId && d.ProformaId != proformaId);

            if (!tieneOtraProforma)
            {
                asignacion.FechaUltimoCobro = asignacion.FechaIngreso;
            }
        }

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

    // Cualquier movimiento (no solo CambioDeEstado) puede marcar la entrada o salida de
    // Dañado: p. ej. "Retornar a bodega" también pone EstadoNuevo=Disponible si el activo
    // estaba Dañado. Filtrar solo por CambioDeEstado dejaba esas salidas sin detectar.
    private async Task<Dictionary<int, List<Movimiento>>> ObtenerCambiosEstadoPorActivoAsync(List<int> activoIds)
    {
        var movimientos = await db.Movimientos
            .AsNoTracking()
            .Where(m => activoIds.Contains(m.ActivoId)
                && (m.EstadoAnterior == EstadoActivo.Danado || m.EstadoNuevo == EstadoActivo.Danado))
            .ToListAsync();

        return movimientos
            .GroupBy(m => m.ActivoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.FechaMovimiento).ToList());
    }

    // Un activo Dañado no se factura mientras lo está: se reconstruyen los tramos "Dañado"
    // de todo el historial de cambios de estado del activo (no solo dentro de la ventana a
    // facturar) para no perder un tramo que empezó antes de FechaUltimoCobro. Si el activo
    // sigue Dañado (no hay cambio de salida registrado), el tramo se extiende hasta el límite
    // pedido (fechaCorte/FechaSalida de la asignación).
    private static List<(DateOnly Desde, DateOnly HastaExclusive)> ObtenerIntervalosDanado(
        List<Movimiento> cambiosEstadoActivo, DateOnly limiteHastaExclusive)
    {
        var intervalos = new List<(DateOnly Desde, DateOnly HastaExclusive)>();
        DateOnly? inicioDanado = null;

        foreach (var m in cambiosEstadoActivo)
        {
            var fecha = DateOnly.FromDateTime(m.FechaMovimiento);
            if (m.EstadoNuevo == EstadoActivo.Danado)
            {
                inicioDanado ??= fecha;
            }
            else if (inicioDanado is not null)
            {
                intervalos.Add((inicioDanado.Value, fecha));
                inicioDanado = null;
            }
        }

        if (inicioDanado is not null)
        {
            intervalos.Add((inicioDanado.Value, limiteHastaExclusive));
        }

        return intervalos;
    }

    // Resta los intervalos "a excluir" (p. ej. tramos Dañado) del rango [desde, hastaExclusive),
    // devolviendo los sub-rangos facturables restantes.
    private static List<(DateOnly Desde, DateOnly HastaExclusive)> RestarIntervalos(
        DateOnly desde, DateOnly hastaExclusive, List<(DateOnly Desde, DateOnly HastaExclusive)> aExcluir)
    {
        var resultado = new List<(DateOnly Desde, DateOnly HastaExclusive)> { (desde, hastaExclusive) };

        foreach (var (excluirDesde, excluirHasta) in aExcluir)
        {
            var siguiente = new List<(DateOnly Desde, DateOnly HastaExclusive)>();
            foreach (var (rangoDesde, rangoHasta) in resultado)
            {
                var solapaDesde = excluirDesde > rangoDesde ? excluirDesde : rangoDesde;
                var solapaHasta = excluirHasta < rangoHasta ? excluirHasta : rangoHasta;

                if (solapaHasta <= solapaDesde)
                {
                    siguiente.Add((rangoDesde, rangoHasta));
                    continue;
                }

                if (rangoDesde < solapaDesde)
                {
                    siguiente.Add((rangoDesde, solapaDesde));
                }
                if (solapaHasta < rangoHasta)
                {
                    siguiente.Add((solapaHasta, rangoHasta));
                }
            }
            resultado = siguiente;
        }

        return resultado;
    }
}
