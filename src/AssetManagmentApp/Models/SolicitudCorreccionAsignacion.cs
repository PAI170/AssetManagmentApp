namespace AssetManagmentApp.Models;

public class SolicitudCorreccionAsignacion
{
    public int Id { get; set; }
    public int AsignacionActivoProyectoId { get; set; }
    public AsignacionActivoProyecto Asignacion { get; set; } = null!;
    public int ProyectoOriginalId { get; set; }
    public Proyecto ProyectoOriginal { get; set; } = null!;
    public int ProyectoNuevoId { get; set; }
    public Proyecto ProyectoNuevo { get; set; } = null!;
    public string Motivo { get; set; } = string.Empty;
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    public int UsuarioSolicitaId { get; set; }
    public ApplicationUser UsuarioSolicita { get; set; } = null!;
    public DateTime FechaSolicitud { get; set; }
    public int? UsuarioResuelveId { get; set; }
    public ApplicationUser? UsuarioResuelve { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? ComentarioResolucion { get; set; }
}
