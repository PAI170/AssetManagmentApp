namespace AssetManagmentApp.Models;

public class TipoEquipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioPorDia { get; set; }
    public string? Descripcion { get; set; }
    public bool Eliminado { get; set; }

    public ICollection<HistorialPrecioTipoEquipo> HistorialPrecios { get; set; } = new List<HistorialPrecioTipoEquipo>();
    public ICollection<Activo> Activos { get; set; } = new List<Activo>();
}
