namespace AssetManagmentApp.Models;

public class ProformaDetalle
{
    public int Id { get; set; }
    public int ProformaId { get; set; }
    public Proforma Proforma { get; set; } = null!;
    public int ActivoId { get; set; }
    public Activo Activo { get; set; } = null!;
    public string TipoEquipoNombre { get; set; } = string.Empty;
    public string? CodigoAlquiler { get; set; }
    public decimal PrecioPorDiaUsado { get; set; }
    public int DiasCobrados { get; set; }
    public decimal Subtotal { get; set; }
}
