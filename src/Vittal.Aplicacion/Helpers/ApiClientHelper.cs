using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Vittal.Aplicacion.Helpers
{
    /// <summary>
    /// Helper centralizado para realizar llamadas HTTP a Vittal.API.
    /// Lee el JWT almacenado en las claims del usuario autenticado e inyecta
    /// el header Authorization: Bearer en cada petición.
    /// </summary>
    public class ApiClientHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiClientHelper> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClientHelper(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApiClientHelper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Crea un HttpClient con el JWT del usuario actual (si está autenticado).
        /// </summary>
        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("VittalApi");

            var jwt = _httpContextAccessor.HttpContext?.User?
                .FindFirst("access_token")?.Value;

            if (!string.IsNullOrEmpty(jwt))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            }

            return client;
        }

        /// <summary>
        /// POST sin autenticación (para login, endpoints públicos).
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> PostAnonymousAsync<T>(
            string endpoint,
            object payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("VittalApi");
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
                    return (true, result, null);
                }

                // Intentar extraer mensaje de error del API
                var errorMsg = ExtractErrorMessage(responseBody);
                _logger.LogWarning("API POST {Endpoint} failed: {Status} — {Error}",
                    endpoint, (int)response.StatusCode, errorMsg);
                return (false, default, errorMsg);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión al llamar a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor. Intente nuevamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar a {Endpoint}", endpoint);
                return (false, default, "Ocurrió un error inesperado.");
            }
        }

        /// <summary>
        /// GET autenticado.
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> GetAsync<T>(string endpoint)
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync(endpoint);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
                    return (true, result, null);
                }

                var errorMsg = ExtractErrorMessage(responseBody);
                _logger.LogWarning("API GET {Endpoint} failed: {Status}", endpoint, (int)response.StatusCode);
                return (false, default, errorMsg);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión al llamar a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar a {Endpoint}", endpoint);
                return (false, default, "Ocurrió un error inesperado.");
            }
        }

        /// <summary>
        /// POST autenticado.
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> PostAsync<T>(
            string endpoint,
            object payload)
        {
            try
            {
                var client = CreateClient();
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
                    return (true, result, null);
                }

                var errorMsg = ExtractErrorMessage(responseBody);
                return (false, default, errorMsg);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión al llamar a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar a {Endpoint}", endpoint);
                return (false, default, "Ocurrió un error inesperado.");
            }
        }

        private static string ExtractErrorMessage(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? "Error desconocido.";
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return err.GetString() ?? "Error desconocido.";
            }
            catch { /* not JSON */ }

            return string.IsNullOrWhiteSpace(responseBody)
                ? "Error desconocido."
                : responseBody[..Math.Min(200, responseBody.Length)];
        }
    }
}
