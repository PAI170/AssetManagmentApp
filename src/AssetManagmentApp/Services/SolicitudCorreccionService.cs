using AssetManagmentApp.Data;
using AssetManagmentApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagmentApp.Services;

public class SolicitudCorreccionService(AppDbContext db)
{
    public async Task<SolicitudCorreccionAsignacion> CrearAsync(int asignacionId, int nuevoProyectoId, string motivo, int usuarioSolicitaId)
    {
        var asignacion = await db.AsignacionesActivoProyecto.FirstOrDefaultAsync(a => a.Id == asignacionId)
            ?? throw new InvalidOperationException("La asignación no existe.");

        ValidarElegible(asignacion);

        if (asignacion.ProyectoId == nuevoProyectoId)
        {
            throw new InvalidOperationException("Seleccioná un proyecto distinto al actual.");
        }

        var yaHayPendiente = await db.SolicitudesCorreccion
            .AnyAsync(s => s.AsignacionActivoProyectoId == asignacionId && s.Estado == EstadoSolicitud.Pendiente);

        if (yaHayPendiente)
        {
            throw new InvalidOperationException("Ya hay una solicitud de corrección pendiente para esta asignación.");
        }

        var solicitud = new SolicitudCorreccionAsignacion
        {
            AsignacionActivoProyectoId = asignacionId,
            ProyectoOriginalId = asignacion.ProyectoId,
            ProyectoNuevoId = nuevoProyectoId,
            Motivo = motivo.Trim(),
            Estado = EstadoSolicitud.Pendiente,
            UsuarioSolicitaId = usuarioSolicitaId,
            FechaSolicitud = DateTime.Now
        };

        db.SolicitudesCorreccion.Add(solicitud);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return solicitud;
    }

    public async Task AprobarAsync(int solicitudId, int usuarioResuelveId)
    {
        var solicitud = await db.SolicitudesCorreccion
            .Include(s => s.ProyectoOriginal)
            .Include(s => s.ProyectoNuevo)
            .FirstOrDefaultAsync(s => s.Id == solicitudId)
            ?? throw new InvalidOperationException("La solicitud no existe.");

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            throw new InvalidOperationException("La solicitud ya fue resuelta.");
        }

        var asignacion = await db.AsignacionesActivoProyecto.FirstOrDefaultAsync(a => a.Id == solicitud.AsignacionActivoProyectoId)
            ?? throw new InvalidOperationException("La asignación ya no existe.");

        try
        {
            ValidarElegible(asignacion);
        }
        catch (InvalidOperationException)
        {
            solicitud.Estado = EstadoSolicitud.Rechazada;
            solicitud.UsuarioResuelveId = usuarioResuelveId;
            solicitud.FechaResolucion = DateTime.Now;
            solicitud.ComentarioResolucion = "Rechazada automáticamente: la asignación ya no cumple las condiciones para corregirse (fue cerrada o ya tiene facturación).";
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            throw new InvalidOperationException("La asignación ya no cumple las condiciones para corregirse (fue cerrada o ya tiene facturación). La solicitud fue rechazada automáticamente.");
        }

        var activo = await db.Activos.FirstOrDefaultAsync(a => a.Id == asignacion.ActivoId)
            ?? throw new InvalidOperationException("El activo ya no existe.");

        var proyectoAnteriorId = asignacion.ProyectoId;
        asignacion.ProyectoId = solicitud.ProyectoNuevoId;

        if (activo.ProyectoActualId == proyectoAnteriorId)
        {
            activo.ProyectoActualId = solicitud.ProyectoNuevoId;
        }

        db.Movimientos.Add(new Movimiento
        {
            ActivoId = asignacion.ActivoId,
            ProyectoId = solicitud.ProyectoNuevoId,
            TipoMovimiento = TipoMovimiento.CorreccionProyecto,
            EstadoAnterior = activo.Estado,
            EstadoNuevo = activo.Estado,
            FechaMovimiento = DateTime.Now,
            UsuarioId = usuarioResuelveId,
            Observacion = $"Corrección de proyecto aprobada: \"{solicitud.ProyectoOriginal.Nombre}\" → \"{solicitud.ProyectoNuevo.Nombre}\". Motivo: {solicitud.Motivo}"
        });

        solicitud.Estado = EstadoSolicitud.Aprobada;
        solicitud.UsuarioResuelveId = usuarioResuelveId;
        solicitud.FechaResolucion = DateTime.Now;

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    public async Task RechazarAsync(int solicitudId, int usuarioResuelveId, string? comentario)
    {
        var solicitud = await db.SolicitudesCorreccion.FirstOrDefaultAsync(s => s.Id == solicitudId)
            ?? throw new InvalidOperationException("La solicitud no existe.");

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            throw new InvalidOperationException("La solicitud ya fue resuelta.");
        }

        solicitud.Estado = EstadoSolicitud.Rechazada;
        solicitud.UsuarioResuelveId = usuarioResuelveId;
        solicitud.FechaResolucion = DateTime.Now;
        solicitud.ComentarioResolucion = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    public Task<int> ContarPendientesAsync() =>
        db.SolicitudesCorreccion.CountAsync(s => s.Estado == EstadoSolicitud.Pendiente);

    // Solo se puede corregir el proyecto de una asignacion abierta (no retornada) que
    // todavia no acumulo ningun dia facturable, para no afectar cobros ya calculados.
    private static void ValidarElegible(AsignacionActivoProyecto asignacion)
    {
        if (asignacion.FechaSalida is not null)
        {
            throw new InvalidOperationException("Esta asignación ya fue cerrada (el activo fue retornado a bodega); no se puede corregir.");
        }

        if (asignacion.FechaUltimoCobro != asignacion.FechaIngreso)
        {
            throw new InvalidOperationException("Ya se generó facturación para esta asignación; no se puede corregir directamente.");
        }
    }
}
