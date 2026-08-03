using System.Text.Json.Serialization;

namespace AssetManagmentApp.Services;

public class TurnstileService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public async Task<bool> VerificarAsync(string? token, string? remoteIp)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var secretKey = configuration["Turnstile:SecretKey"]
            ?? throw new InvalidOperationException("Turnstile:SecretKey no configurada.");

        var parametros = new Dictionary<string, string>
        {
            ["secret"] = secretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            parametros["remoteip"] = remoteIp;
        }

        using var client = httpClientFactory.CreateClient();
        using var respuesta = await client.PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            new FormUrlEncodedContent(parametros));

        if (!respuesta.IsSuccessStatusCode)
        {
            return false;
        }

        var resultado = await respuesta.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();
        return resultado?.Success ?? false;
    }

    private class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
