using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Catalogos.Controllers
{
    /// <summary>
    /// DTO interno para recibir datos del formulario de diagnósticos desde el cliente.
    /// </summary>
    public class DiagnosticoFormDto
    {
        public string CitaId { get; set; } = string.Empty;
        public string TipoDiagnosticoId { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    [Area("Catalogos")]
    [Authorize]
    public class DiagnosticoController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<DiagnosticoController> _logger;

        public DiagnosticoController(ApiClientHelper apiClient, ILogger<DiagnosticoController> logger)
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
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Diagnosticos/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Diagnóstico no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Diagnostico = data;
            return View();
        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todos los diagnósticos — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonListar([FromQuery] bool inactivos = false)
        {
            var url = inactivos ? "api/Diagnosticos?inactivos=true" : "api/Diagnosticos";
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)
            {
                _logger.LogWarning("JsonListar API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar diagnósticos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Busca diagnósticos por término — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonBuscar([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = true, data = new List<object>() });
            }

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Diagnosticos/buscar?q={Uri.EscapeDataString(q)}");

            if (!success)
            {
                _logger.LogWarning("JsonBuscar API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al buscar diagnósticos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene tipos de diagnóstico para el dropdown — para fetch() desde Create/Edit</summary>
        [HttpGet]
        public async Task<IActionResult> JsonTiposDiagnostico()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/TiposDiagnostico");

            if (!success)
            {
                _logger.LogWarning("JsonTiposDiagnostico API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = new List<object>() });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Crea un nuevo diagnóstico — para fetch() desde la vista Create</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrear([FromBody] DiagnosticoFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TipoDiagnosticoId) || !Guid.TryParse(dto.TipoDiagnosticoId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un tipo de diagnóstico." });
            }

            _logger.LogInformation("JsonCrear called: tipoDiagnosticoId={TipoDiagnosticoId}, citaId={CitaId}",
                dto.TipoDiagnosticoId, dto.CitaId);

            var payload = new
            {
                citaId = dto.CitaId,
                tipoDiagnosticoId = dto.TipoDiagnosticoId,
                descripcion = dto.Descripcion
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Diagnosticos", payload);

            if (!success)
            {
                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear diagnóstico" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Diagnóstico creado exitosamente" });
        }

        /// <summary>Actualiza un diagnóstico — para fetch() desde la vista Edit</summary>
        [HttpPut]
        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] DiagnosticoFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TipoDiagnosticoId) || !Guid.TryParse(dto.TipoDiagnosticoId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un tipo de diagnóstico." });
            }

            _logger.LogInformation("JsonActualizar called: id={Id}, tipoDiagnosticoId={TipoDiagnosticoId}", id, dto.TipoDiagnosticoId);

            var payload = new
            {
                citaId = dto.CitaId,
                tipoDiagnosticoId = dto.TipoDiagnosticoId,
                descripcion = dto.Descripcion
            };

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Diagnosticos/{id}", payload);

            if (!success)
            {
                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar diagnóstico" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Diagnóstico actualizado exitosamente" });
        }

        /// <summary>Desactiva un diagnóstico — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivar(Guid id)
        {
            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Diagnosticos/{id}/desactivar", null);

            if (!success)
            {
                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar diagnóstico" });
            }

            return Ok(new { success = true, message = "Diagnóstico desactivado exitosamente" });
        }

        /// <summary>Reactiva un diagnóstico — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonReactivar(Guid id)
        {
            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Diagnosticos/{id}/reactivar", null);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar diagnóstico" });
            }

            return Ok(new { success = true, message = "Diagnóstico reactivado exitosamente" });
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
                if (response.Value.ValueKind == JsonValueKind.Array)
                {
                    return EnumerateJsonArray(response.Value);
                }
            }
            catch { }

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
