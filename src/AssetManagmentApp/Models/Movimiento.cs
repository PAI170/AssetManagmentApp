namespace AssetManagmentApp.Models;

public class Movimiento
{
    public int Id { get; set; }
    public int ActivoId { get; set; }
    public Activo Activo { get; set; } = null!;
    public int? ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }
    public TipoMovimiento TipoMovimiento { get; set; }
    public EstadoActivo EstadoAnterior { get; set; }
    public EstadoActivo EstadoNuevo { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public int UsuarioId { get; set; }
    public ApplicationUser Usuario { get; set; } = null!;
    public string? Observacion { get; set; }
}
