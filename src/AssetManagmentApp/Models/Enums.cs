namespace AssetManagmentApp.Models;

public enum RolUsuario
{
    Admin,
    Consultor
}

public enum EstadoProyecto
{
    Activo,
    Cerrado
}

public enum EstadoActivo
{
    Disponible,
    Asignado,
    EnReparacion,
    Danado
}

public enum TipoMovimiento
{
    Asignacion,
    RetornoABodega,
    CambioDeEstado
}

public enum EstadoProforma
{
    Generada,
    Anulada
}

public static class EnumEtiquetas
{
    public static string Etiqueta(this EstadoActivo estado) => estado switch
    {
        EstadoActivo.Disponible => "Disponible",
        EstadoActivo.Asignado => "Asignado",
        EstadoActivo.EnReparacion => "En Reparación",
        EstadoActivo.Danado => "Dañado",
        _ => estado.ToString()
    };

    public static string Etiqueta(this TipoMovimiento tipo) => tipo switch
    {
        TipoMovimiento.Asignacion => "Asignación",
        TipoMovimiento.RetornoABodega => "Retorno a bodega",
        TipoMovimiento.CambioDeEstado => "Cambio de estado",
        _ => tipo.ToString()
    };
}
