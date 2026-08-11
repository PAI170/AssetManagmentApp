using System.Text.Json.Serialization;

namespace AssetManagmentApp.Services;

public class TipoCambioService(IHttpClientFactory httpClientFactory, ILogger<TipoCambioService> logger)
{
    // Espeja el tipo de cambio de referencia que publica el Banco Central de Costa Rica
    // (venta) sin requerir el correo/token de suscripción de su servicio web oficial.
    private const string Endpoint = "https://apis.gometa.org/tdc/tdc.json";

    public async Task<decimal?> ObtenerVentaDelDiaAsync()
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            var respuesta = await client.GetFromJsonAsync<TipoCambioRespuesta>(Endpoint);
            return respuesta?.Venta;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo obtener el tipo de cambio del día.");
            return null;
        }
    }

    private class TipoCambioRespuesta
    {
        [JsonPropertyName("venta")]
        public decimal Venta { get; set; }
    }
}
