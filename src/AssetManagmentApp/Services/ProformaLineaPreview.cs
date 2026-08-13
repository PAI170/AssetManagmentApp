namespace AssetManagmentApp.Services;

public class ProformaLineaPreview
{
    public int AsignacionId { get; set; }
    public int ActivoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string TipoEquipoNombre { get; set; } = string.Empty;
    public string? CodigoAlquiler { get; set; }
    public decimal PrecioPorDiaUsado { get; set; }
    public int DiasCobrados { get; set; }
    public decimal Subtotal { get; set; }
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHastaExclusive { get; set; }
    public DateOnly NuevaFechaUltimoCobro { get; set; }
}
