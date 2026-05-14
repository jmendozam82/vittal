using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

using Vittal.Aplicacion.Helpers;


namespace Vittal.Aplicacion.Areas.Catalogos.Controllers

{

    /// <summary>
    /// DTO interno para recibir datos del formulario de tipos de antecedente desde el cliente.
    /// </summary>

    public class TipoAntecedenteFormDto

    {

        public Guid SalaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Categoria { get; set; }

        public string TipoDato { get; set; } = "boolean";

        public int Orden { get; set; }

    }


    [Area("Catalogos")]

    [Authorize]

    public class TipoAntecedenteController : Controller

    {

        private readonly ApiClientHelper _apiClient;

        private readonly ILogger<TipoAntecedenteController> _logger;


        public TipoAntecedenteController(ApiClientHelper apiClient, ILogger<TipoAntecedenteController> logger)

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

            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/TipoAntecedente/{id}");


            if (!success)

            {

                TempData["Error"] = "Error al conectar con la API.";

                return RedirectToAction("Index");

            }


            var data = ExtractDataObject(response);

            if (data == null)

            {

                TempData["Error"] = "Tipo de antecedente no encontrado.";

                return RedirectToAction("Index");

            }


            ViewBag.TipoAntecedente = data;

            return View();

        }


        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================


        /// <summary>Lista todas las salas activas — para el dropdown de selector de sala</summary>

        [HttpGet]

        public async Task<IActionResult> JsonSalas()

        {

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Salas");


            if (!success)

            {

                _logger.LogWarning("JsonSalas API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar salas" });

            }


            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }


        /// <summary>Lista todos los tipos de antecedente de una sala — para fetch() desde la vista Index</summary>

        [HttpGet]

        public async Task<IActionResult> JsonListar([FromQuery] Guid salaId)

        {

            if (salaId == Guid.Empty)

            {

                return Json(new { success = true, data = new List<object>() });

            }


            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/TipoAntecedente/sala/{salaId}");


            if (!success)

            {

                _logger.LogWarning("JsonListar API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar tipos de antecedente" });

            }


            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }


        /// <summary>Crea un nuevo tipo de antecedente — para fetch() desde la vista Create</summary>

        [HttpPost]

        public async Task<IActionResult> JsonCrear([FromBody] TipoAntecedenteFormDto dto)

        {

            if (dto.SalaId == Guid.Empty)

            {

                return BadRequest(new { success = false, message = "Debe seleccionar una sala." });

            }

            if (string.IsNullOrWhiteSpace(dto.Nombre))

            {

                return BadRequest(new { success = false, message = "El nombre del tipo de antecedente es obligatorio." });

            }


            _logger.LogInformation("JsonCrear called: nombre={Nombre}, salaId={SalaId}", dto.Nombre, dto.SalaId);


            var payload = new

            {

                salaId = dto.SalaId,

                nombre = dto.Nombre,

                categoria = dto.Categoria,

                tipoDato = dto.TipoDato,

                orden = dto.Orden

            };


            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/TipoAntecedente", payload);


            if (!success)

            {

                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear tipo de antecedente" });

            }


            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Tipo de antecedente creado exitosamente" });

        }


        /// <summary>Actualiza un tipo de antecedente — para fetch() desde la vista Edit</summary>

        [HttpPut]

        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] TipoAntecedenteFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

            {

                return BadRequest(new { success = false, message = "El nombre del tipo de antecedente es obligatorio." });

            }


            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre}", id, dto.Nombre);


            var payload = new

            {

                salaId = dto.SalaId,

                nombre = dto.Nombre,

                categoria = dto.Categoria,

                tipoDato = dto.TipoDato,

                orden = dto.Orden

            };


            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/TipoAntecedente/{id}", payload);


            if (!success)

            {

                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar tipo de antecedente" });

            }


            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Tipo de antecedente actualizado exitosamente" });

        }


        /// <summary>Desactiva un tipo de antecedente — para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonDesactivar(Guid id)

        {

            _logger.LogInformation("JsonDesactivar called: id={Id}", id);


            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/TipoAntecedente/{id}/desactivar", null);


            if (!success)

            {

                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar tipo de antecedente" });

            }


            return Ok(new { success = true, message = "Tipo de antecedente desactivado exitosamente" });

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
