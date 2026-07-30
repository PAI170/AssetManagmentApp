namespace AssetManagmentApp.Models;

public class AsignacionActivoProyecto
{
    public int Id { get; set; }
    public int ActivoId { get; set; }
    public Activo Activo { get; set; } = null!;
    public int ProyectoId { get; set; }
    public Proyecto Proyecto { get; set; } = null!;
    public DateOnly FechaIngreso { get; set; }
    public DateOnly? FechaSalida { get; set; }
    public DateOnly FechaUltimoCobro { get; set; }
}
