namespace AssetManagmentApp.Models;

public class HistorialPrecioTipoEquipo
{
    public int Id { get; set; }
    public int TipoEquipoId { get; set; }
    public TipoEquipo TipoEquipo { get; set; } = null!;
    public decimal Precio { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
}
