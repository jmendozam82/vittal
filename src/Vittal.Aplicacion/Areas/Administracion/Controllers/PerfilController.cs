using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Administracion.Controllers
{
    /// <summary>
    /// DTO interno para recibir datos del formulario de perfiles desde el cliente.
    /// </summary>
    public class PerfilFormDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool EsAdmin { get; set; }
    }

    [Area("Administracion")]
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<PerfilController> _logger;

        public PerfilController(ApiClientHelper apiClient, ILogger<PerfilController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ===================== VISTAS (Server-side rendering) =====================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Perfiles/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Perfil no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Perfil = data;
            return View();
        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todos los perfiles — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonPerfiles([FromQuery] bool inactivos = false)
        {
            var url = inactivos ? "api/Perfiles?inactivos=true" : "api/Perfiles";
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)
            {
                _logger.LogWarning("JsonPerfiles API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar perfiles" });
            }

            // Extraer el array "data" del JsonElement
            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Crea un nuevo perfil — para fetch() desde la vista Create</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrear([FromBody] PerfilFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre es obligatorio." });
            }

            _logger.LogInformation("JsonCrear called: nombre={Nombre}, esAdmin={EsAdmin}", dto.Nombre, dto.EsAdmin);

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Perfiles",
                new { nombre = dto.Nombre, descripcion = dto.Descripcion, esAdmin = dto.EsAdmin });

            if (!success)
            {
                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear perfil" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Perfil creado exitosamente" });
        }

        /// <summary>Actualiza un perfil — para fetch() desde la vista Edit</summary>
        [HttpPut]
        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] PerfilFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre es obligatorio." });
            }

            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre}", id, dto.Nombre);

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Perfiles/{id}",
                new { nombre = dto.Nombre, descripcion = dto.Descripcion, esAdmin = dto.EsAdmin });

            if (!success)
            {
                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar perfil" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Perfil actualizado exitosamente" });
        }

        /// <summary>Desactiva un perfil — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivar(Guid id)
        {
            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Perfiles/{id}/desactivar", null);

            if (!success)
            {
                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar perfil" });
            }

            return Ok(new { success = true, message = "Perfil desactivado exitosamente" });
        }

        /// <summary>Reactiva un perfil -- para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonReactivar(Guid id)
        {
            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Perfiles/{id}/reactivar", null);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar perfil" });
            }

            return Ok(new { success = true, message = "Perfil reactivado exitosamente" });
        }

        // ========== Helpers para extraer data de JsonElement ==========

        private static IEnumerable<object> ExtractDataArray(JsonElement? response)
        {
            if (!response.HasValue) return new List<object>();

            try
            {
                if (response.Value.TryGetProperty("data", out var dataProp))
                {
                    return EnumerateJsonArray(dataProp);
                }
                // Si la respuesta ya es un array directamente
                if (response.Value.ValueKind == JsonValueKind.Array)
                {
                    return EnumerateJsonArray(response.Value);
                }
            }
            catch { /* return empty */ }

            return new List<object>();
        }

        private static object? ExtractDataObject(JsonElement? response)
        {
            if (!response.HasValue) return null;

            try
            {
                if (response.Value.TryGetProperty("data", out var dataProp))
                {
                    return JsonElementToDictionary(dataProp);
                }
                return JsonElementToDictionary(response.Value);
            }
            catch { return null; }
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
