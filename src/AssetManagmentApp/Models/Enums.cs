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
    CambioDeEstado,
    CorreccionProyecto
}

public enum EstadoProforma
{
    Generada,
    Anulada
}

public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada
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
        TipoMovimiento.CorreccionProyecto => "Corrección de asignación",
        _ => tipo.ToString()
    };

    public static string Etiqueta(this EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.Pendiente => "Pendiente",
        EstadoSolicitud.Aprobada => "Aprobada",
        EstadoSolicitud.Rechazada => "Rechazada",
        _ => estado.ToString()
    };
}
