namespace AssetManagmentApp.Models;

public class Activo
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public int TipoEquipoId { get; set; }
    public TipoEquipo TipoEquipo { get; set; } = null!;
    public EstadoActivo Estado { get; set; } = EstadoActivo.Disponible;
    public int? ProyectoActualId { get; set; }
    public Proyecto? ProyectoActual { get; set; }
    public DateOnly FechaRegistro { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
    public ICollection<AsignacionActivoProyecto> Asignaciones { get; set; } = new List<AsignacionActivoProyecto>();
    public ICollection<ProformaDetalle> DetallesProforma { get; set; } = new List<ProformaDetalle>();
}
