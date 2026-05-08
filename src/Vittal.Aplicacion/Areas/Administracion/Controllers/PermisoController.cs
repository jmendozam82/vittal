using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Administracion.Controllers
{
    /// <summary>
    /// DTO interno para recibir la actualización de permisos desde el formulario.
    /// </summary>
    public class PermisoFormItem
    {
        public string ModuloId { get; set; } = string.Empty;
        public bool PuedeLeer { get; set; }
        public bool PuedeCrear { get; set; }
        public bool PuedeActualizar { get; set; }
    }

    public class PermisoGuardarDto
    {
        public List<PermisoFormItem> Permisos { get; set; } = new();
    }

    [Area("Administracion")]
    [Authorize]
    public class PermisoController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<PermisoController> _logger;

        public PermisoController(ApiClientHelper apiClient, ILogger<PermisoController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// Página principal de gestión de permisos.
        /// Muestra selector de perfil y tabla de módulos con checkboxes.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ===================== JSON PROXY ENDPOINTS =====================

        /// <summary>
        /// Obtiene todos los perfiles activos de la clínica para el selector.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> JsonPerfiles()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Perfiles");

            if (!success)
            {
                _logger.LogWarning("JsonPerfiles API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar perfiles" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>
        /// Obtiene los permisos de un perfil específico (con todos los módulos).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> JsonPermisos(Guid perfilId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Permisos/perfil/{perfilId}");

            if (!success)
            {
                _logger.LogWarning("JsonPermisos API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar permisos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>
        /// Guarda los permisos de un perfil (batch upsert).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> JsonGuardar(Guid perfilId, [FromBody] PermisoGuardarDto dto)
        {
            if (dto == null || dto.Permisos == null || dto.Permisos.Count == 0)
            {
                return BadRequest(new { success = false, message = "No se recibieron permisos para guardar." });
            }

            _logger.LogInformation("JsonGuardar called: perfilId={PerfilId}, count={Count}",
                perfilId, dto.Permisos.Count);

            // Convertir a formato que espera el API
            var payload = new
            {
                permisos = dto.Permisos.Select(p => new
                {
                    moduloId = p.ModuloId,
                    puedeLeer = p.PuedeLeer,
                    puedeCrear = p.PuedeCrear,
                    puedeActualizar = p.PuedeActualizar
                }).ToList()
            };

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>(
                $"api/Permisos/perfil/{perfilId}", payload);

            if (!success)
            {
                _logger.LogWarning("JsonGuardar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al guardar permisos" });
            }

            return Ok(new { success = true, message = "Permisos guardados exitosamente" });
        }

        // ===================== HELPERS =====================

        private static IEnumerable<object> ExtractDataArray(JsonElement? response)
        {
            if (!response.HasValue) return new List<object>();

            try
            {
                if (response.Value.TryGetProperty("data", out var dataProp))
                {
                    return EnumerateJsonArray(dataProp);
                }
                if (response.Value.ValueKind == JsonValueKind.Array)
                {
                    return EnumerateJsonArray(response.Value);
                }
            }
            catch { }

            return new List<object>();
        }

        private static IEnumerable<object> EnumerateJsonArray(JsonElement array)
        {
            var list = new List<object>();
            foreach (var item in array.EnumerateArray())
            {
                list.Add(JsonElementToDictionary(item));
            }
            return list;
        }

        private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object?>();
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = JsonElementToValue(prop.Value);
                }
            }
            return dict;
        }

        private static object? JsonElementToValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => JsonElementToDictionary(element),
                JsonValueKind.Array => EnumerateJsonArray(element).ToList(),
                _ => element.GetRawText()
            };
        }
    }
}
