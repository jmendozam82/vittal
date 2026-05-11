using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Catalogos.Controllers
{
    /// <summary>
    /// DTO interno para recibir datos del formulario de medicamentos desde el cliente.
    /// </summary>
    public class MedicamentoFormDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Concentracion { get; set; }
        public string? UnidadMedida { get; set; }
    }

    [Area("Catalogos")]
    [Authorize]
    public class MedicamentoController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<MedicamentoController> _logger;

        public MedicamentoController(ApiClientHelper apiClient, ILogger<MedicamentoController> logger)
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
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Medicamentos/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Medicamento no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Medicamento = data;
            return View();
        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todos los medicamentos — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonMedicamentos([FromQuery] bool inactivos = false)
        {
            var url = inactivos ? "api/Medicamentos?inactivos=true" : "api/Medicamentos";
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)
            {
                _logger.LogWarning("JsonMedicamentos API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar medicamentos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Busca medicamentos por término — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonBuscar([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = true, data = new List<object>() });
            }

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(
                $"api/Medicamentos/buscar?q={Uri.EscapeDataString(q)}");

            if (!success)
            {
                _logger.LogWarning("JsonBuscar API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al buscar medicamentos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Crea un nuevo medicamento — para fetch() desde la vista Create</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrear([FromBody] MedicamentoFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre del medicamento es obligatorio." });
            }

            _logger.LogInformation("JsonCrear called: nombre={Nombre}", dto.Nombre);

            var payload = new
            {
                nombre = dto.Nombre,
                descripcion = dto.Descripcion,
                concentracion = dto.Concentracion,
                unidadMedida = dto.UnidadMedida
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Medicamentos", payload);

            if (!success)
            {
                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear medicamento" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Medicamento creado exitosamente" });
        }

        /// <summary>Actualiza un medicamento — para fetch() desde la vista Edit</summary>
        [HttpPut]
        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] MedicamentoFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre del medicamento es obligatorio." });
            }

            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre}", id, dto.Nombre);

            var payload = new
            {
                nombre = dto.Nombre,
                descripcion = dto.Descripcion,
                concentracion = dto.Concentracion,
                unidadMedida = dto.UnidadMedida
            };

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Medicamentos/{id}", payload);

            if (!success)
            {
                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar medicamento" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Medicamento actualizado exitosamente" });
        }

        /// <summary>Desactiva un medicamento — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivar(Guid id)
        {
            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Medicamentos/{id}/desactivar", null);

            if (!success)
            {
                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar medicamento" });
            }

            return Ok(new { success = true, message = "Medicamento desactivado exitosamente" });
        }

        /// <summary>Reactiva un medicamento — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonReactivar(Guid id)
        {
            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Medicamentos/{id}/reactivar", null);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar medicamento" });
            }

            return Ok(new { success = true, message = "Medicamento reactivado exitosamente" });
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
