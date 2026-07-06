using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Administracion.Controllers
{
    /// <summary>
    /// DTO interno para recibir datos del formulario de plantillas desde el cliente.
    /// </summary>
    public class PlantillaEspecialidadFormDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Icono { get; set; }
    }

    [Area("Administracion")]
    [Authorize]
    public class PlantillaEspecialidadController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<PlantillaEspecialidadController> _logger;

        public PlantillaEspecialidadController(ApiClientHelper apiClient, ILogger<PlantillaEspecialidadController> logger)
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
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/PlantillaEspecialidad/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Plantilla no encontrada.";
                return RedirectToAction("Index");
            }

            ViewBag.Plantilla = data;
            return View();
        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todas las plantillas de especialidad — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonListar()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/PlantillaEspecialidad");

            if (!success)
            {
                _logger.LogWarning("JsonListar API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar plantillas" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Crea una nueva plantilla — para fetch() desde la vista Create</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrear([FromBody] PlantillaEspecialidadFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre es obligatorio." });
            }

            _logger.LogInformation("JsonCrear called: nombre={Nombre}", dto.Nombre);

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/PlantillaEspecialidad",
                new { nombre = dto.Nombre, descripcion = dto.Descripcion, icono = dto.Icono });

            if (!success)
            {
                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear plantilla" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Plantilla creada exitosamente" });
        }

        /// <summary>Actualiza una plantilla — para fetch() desde la vista Edit</summary>
        [HttpPut]
        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] PlantillaEspecialidadFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre es obligatorio." });
            }

            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre}", id, dto.Nombre);

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/PlantillaEspecialidad/{id}",
                new { nombre = dto.Nombre, descripcion = dto.Descripcion, icono = dto.Icono });

            if (!success)
            {
                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar plantilla" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Plantilla actualizada exitosamente" });
        }

        /// <summary>Desactiva una plantilla — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivar(Guid id)
        {
            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/PlantillaEspecialidad/{id}/desactivar", null);

            if (!success)
            {
                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar plantilla" });
            }

            return Ok(new { success = true, message = "Plantilla desactivada exitosamente" });
        }

        /// <summary>Reactiva una plantilla — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonReactivar(Guid id)
        {
            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/PlantillaEspecialidad/{id}/reactivar", null);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar plantilla" });
            }

            return Ok(new { success = true, message = "Plantilla reactivada exitosamente" });
        }

        // ===================== ITEMS PROXY ENDPOINTS =====================

        /// <summary>Lista los items de una plantilla — para fetch() desde Edit</summary>
        [HttpGet]
        public async Task<IActionResult> JsonListarItems(Guid plantillaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/PlantillaItem/plantilla/{plantillaId}");

            if (!success)
            {
                return Json(new { success = false, message = errorMessage ?? "Error al cargar items" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Crea un item en una plantilla — para fetch() desde Edit</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearItem([FromBody] JsonElement body)
        {
            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/PlantillaItem", body);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear item" });
            }

            return Ok(new { success = true, message = "Item creado exitosamente" });
        }

        /// <summary>Actualiza un item — para fetch() desde Edit</summary>
        [HttpPut]
        public async Task<IActionResult> JsonActualizarItem(Guid id, [FromBody] JsonElement body)
        {
            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/PlantillaItem/{id}", body);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar item" });
            }

            return Ok(new { success = true, message = "Item actualizado exitosamente" });
        }

        /// <summary>Desactiva un item — para fetch() desde Edit</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivarItem(Guid id)
        {
            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/PlantillaItem/{id}/desactivar", null);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar item" });
            }

            return Ok(new { success = true, message = "Item desactivado exitosamente" });
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
