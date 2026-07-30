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
