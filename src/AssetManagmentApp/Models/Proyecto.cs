namespace AssetManagmentApp.Models;

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string IngenieroACargo { get; set; } = string.Empty;
    public DateOnly FechaCreacion { get; set; }
    public EstadoProyecto Estado { get; set; } = EstadoProyecto.Activo;

    public ICollection<Activo> ActivosAsignados { get; set; } = new List<Activo>();
    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
    public ICollection<AsignacionActivoProyecto> Asignaciones { get; set; } = new List<AsignacionActivoProyecto>();
    public ICollection<Proforma> Proformas { get; set; } = new List<Proforma>();
}
