namespace AssetManagmentApp.Models;

public class Proforma
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public Proyecto Proyecto { get; set; } = null!;
    public DateOnly FechaGeneracion { get; set; }
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHasta { get; set; }
    public decimal Total { get; set; }
    public int UsuarioGeneroId { get; set; }
    public ApplicationUser UsuarioGenero { get; set; } = null!;
    public bool EnviadaPorCorreo { get; set; }
    public EstadoProforma Estado { get; set; } = EstadoProforma.Generada;

    public ICollection<ProformaDetalle> Detalles { get; set; } = new List<ProformaDetalle>();
}
