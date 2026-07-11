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
        /// Lee primero de la cookie separada "vittal_jwt" (fuente principal),
        /// con fallback al claim "access_token" por compatibilidad.
        /// Usa HttpRequestMessage con headers explícitos para evitar problemas
        /// con el pooling de IHttpClientFactory.
        /// </summary>
        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("VittalApi");
            var jwt = GetJwtFromCookieOrClaim();

            if (!string.IsNullOrEmpty(jwt))
            {
                // Remove any existing Authorization header first (pooled clients may have stale headers)
                client.DefaultRequestHeaders.Authorization = null;
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                _logger.LogInformation("CreateClient - Authorization header set: Bearer {Length} chars",
                    jwt.Length);
            }
            else
            {
                _logger.LogWarning("CreateClient - No JWT disponible");
            }

            return client;
        }

        /// <summary>
        /// Obtiene el JWT de la cookie HttpOnly o del claim como fallback.
        /// </summary>
        private string? GetJwtFromCookieOrClaim()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Método principal: cookie HttpOnly separada
            var jwt = httpContext?.Request.Cookies["vittal_jwt"];

            // Fallback: claim (por compatibilidad)
            if (string.IsNullOrEmpty(jwt))
            {
                jwt = httpContext?.User?.FindFirst("access_token")?.Value;
            }

            return jwt;
        }

        /// <summary>
        /// Ejecuta una petición HTTP con el JWT inyectado via HttpRequestMessage.
        /// Más confiable que DefaultRequestHeaders con IHttpClientFactory.
        /// </summary>
        private async Task<HttpResponseMessage> SendAuthenticatedRequestAsync(
            HttpMethod method,
            string endpoint,
            HttpContent? content = null)
        {
            var client = _httpClientFactory.CreateClient("VittalApi");
            var jwt = GetJwtFromCookieOrClaim();
            var httpContext = _httpContextAccessor.HttpContext;

            var request = new HttpRequestMessage(method, endpoint);

            if (!string.IsNullOrEmpty(jwt))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
                _logger.LogInformation("SendAuthenticatedRequest - {Method} {Endpoint} - Bearer {Length} chars",
                    method, endpoint, jwt.Length);
            }
            else
            {
                _logger.LogWarning("SendAuthenticatedRequest - No JWT para {Method} {Endpoint}",
                    method, endpoint);
            }

            // Si el Super Admin tiene una clínica override en sesión, agregar header
            var overrideClinicaId = httpContext?.Session?.GetString("ClinicaOverride");
            if (!string.IsNullOrEmpty(overrideClinicaId) && Guid.TryParse(overrideClinicaId, out _))
            {
                request.Headers.Add("X-Clinica-Override", overrideClinicaId);
                _logger.LogInformation("SendAuthenticatedRequest - X-Clinica-Override: {ClinicaId}", overrideClinicaId);
            }

            if (content != null)
            {
                request.Content = content;
            }

            return await client.SendAsync(request);
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
                var response = await SendAuthenticatedRequestAsync(HttpMethod.Get, endpoint);
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
            object? payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendAuthenticatedRequestAsync(HttpMethod.Post, endpoint, content);
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

        /// <summary>
        /// PUT autenticado (actualizar recurso).
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> PutAsync<T>(
            string endpoint,
            object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendAuthenticatedRequestAsync(HttpMethod.Put, endpoint, content);
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
                _logger.LogError(ex, "Error de conexi\u00f3n al llamar a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar a {Endpoint}", endpoint);
                return (false, default, "Ocurri\u00f3 un error inesperado.");
            }
        }

        /// <summary>
        /// PATCH autenticado (actualizaci\u00f3n parcial).
        /// </summary>
        /// <summary>
        /// POST multipart/form-data autenticado (para subida de archivos).
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> PostFileAsync<T>(
            string endpoint,
            string fileName,
            Stream fileStream,
            string contentType,
            string fieldName = "file")
        {
            try
            {
                using var formContent = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                formContent.Add(streamContent, fieldName, fileName);

                var response = await SendAuthenticatedRequestAsync(HttpMethod.Post, endpoint, formContent);
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
                _logger.LogError(ex, "Error de conexión al subir archivo a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al subir archivo a {Endpoint}", endpoint);
                return (false, default, "Ocurrió un error inesperado.");
            }
        }

        /// <summary>
        /// POST multipart/form-data autenticado con campos adicionales.
        /// Para subir archivos con metadata (expedienteId, hojaCitaId, etc.)
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> PostMultipartAsync<T>(
            string endpoint,
            string fileName,
            Stream fileStream,
            string contentType,
            Dictionary<string, string>? fields = null,
            string fileFieldName = "file")
        {
            try
            {
                using var formContent = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                formContent.Add(streamContent, fileFieldName, fileName);

                if (fields != null)
                {
                    foreach (var kvp in fields)
                    {
                        formContent.Add(new StringContent(kvp.Value), kvp.Key);
                    }
                }

                var response = await SendAuthenticatedRequestAsync(HttpMethod.Post, endpoint, formContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
                    return (true, result, null);
                }

                var errorMsg = ExtractErrorMessage(responseBody);
                _logger.LogWarning("API POST multipart {Endpoint} failed: {Status} - {Error}",
                    endpoint, (int)response.StatusCode, errorMsg);
                return (false, default, errorMsg);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexion al subir archivo a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al subir archivo a {Endpoint}", endpoint);
                return (false, default, "Ocurrio un error inesperado.");
            }
        }

        /// <summary>
        /// PATCH autenticado (actualizaci\u00f3n parcial).
        /// </summary>
        public async Task<(bool Success, T? Data, string? ErrorMessage)> PatchAsync<T>(
            string endpoint,
            object? payload)
        {
            try
            {
                var json = payload != null ? JsonSerializer.Serialize(payload) : "{}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendAuthenticatedRequestAsync(new HttpMethod("PATCH"), endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = string.IsNullOrWhiteSpace(responseBody)
                        ? default
                        : JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
                    return (true, result, null);
                }

                var errorMsg = ExtractErrorMessage(responseBody);
                return (false, default, errorMsg);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexi\u00f3n al llamar a {Endpoint}", endpoint);
                return (false, default, "No se pudo conectar con el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar a {Endpoint}", endpoint);
                return (false, default, "Ocurri\u00f3 un error inesperado.");
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
