using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Catalogos.Controllers

{

    /// <summary>

    /// DTO interno para recibir datos del formulario de clínicas desde el cliente.

    /// </summary>

    public class ClinicaFormDto

    {

        public string Nombre { get; set; } = string.Empty;

        public string? Direccion { get; set; }

        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? LogoUrl { get; set; }

        public int TiempoEsperaMinutos { get; set; } = 30;

        public string? BdExterna1 { get; set; }

        public string? BdExterna2 { get; set; }

        public string? HorarioApertura { get; set; }

        public string? HorarioCierre { get; set; }

        public string? DiasAtencion { get; set; }

    }

    [Area("Catalogos")]

    [Authorize]

    public class ClinicaController : Controller

    {

        private readonly ApiClientHelper _apiClient;

        private readonly ILogger<ClinicaController> _logger;

        public ClinicaController(ApiClientHelper apiClient, ILogger<ClinicaController> logger)

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

            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Clinicas/{id}");

            if (!success)

            {

                TempData["Error"] = "Error al conectar con la API.";

                return RedirectToAction("Index");

            }

            var data = ExtractDataObject(response);

            if (data == null)

            {

                TempData["Error"] = "Clínica no encontrada.";

                return RedirectToAction("Index");

            }

            ViewBag.Clinica = data;

            return View();

        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todas las clínicas — para fetch() desde la vista Index</summary>

        [HttpGet]

        public async Task<IActionResult> JsonClinicas([FromQuery] bool inactivos = false)

        {

            var url = inactivos ? "api/Clinicas?inactivos=true" : "api/Clinicas";

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)

            {

                _logger.LogWarning("JsonClinicas API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar clínicas" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Obtiene la clínica actual del usuario — para fetch()</summary>

        [HttpGet]

        public async Task<IActionResult> JsonMiClinica()

        {

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Clinicas/mi-clínica");

            if (!success)

            {

                _logger.LogWarning("JsonMiClinica API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar datos de la clínica" });

            }

            var data = ExtractDataObject(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Crea una nueva clínica — para fetch() desde la vista Create</summary>

        [HttpPost]

        public async Task<IActionResult> JsonCrear([FromBody] ClinicaFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

            {

                return BadRequest(new { success = false, message = "El nombre de la clínica es obligatorio." });

            }

            if (dto.Nombre.Length < 3)

            {

                return BadRequest(new { success = false, message = "El nombre debe tener al menos 3 caracteres." });

            }

            _logger.LogInformation("JsonCrear called: nombre={Nombre}", dto.Nombre);

            var payload = new

            {

                nombre = dto.Nombre,

                direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion,

                telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono,

                email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email,

                logoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl,

                tiempoEsperaMinutos = dto.TiempoEsperaMinutos > 0 ? dto.TiempoEsperaMinutos : 30,

                bdExterna1 = string.IsNullOrWhiteSpace(dto.BdExterna1) ? null : dto.BdExterna1,

                bdExterna2 = string.IsNullOrWhiteSpace(dto.BdExterna2) ? null : dto.BdExterna2,

                horarioApertura = string.IsNullOrWhiteSpace(dto.HorarioApertura) ? null : dto.HorarioApertura,

                horarioCierre = string.IsNullOrWhiteSpace(dto.HorarioCierre) ? null : dto.HorarioCierre,

                diasAtencion = string.IsNullOrWhiteSpace(dto.DiasAtencion) ? null : dto.DiasAtencion

            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Clinicas", payload);

            if (!success)

            {

                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear clínica" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Clínica creada exitosamente" });

        }

        /// <summary>Actualiza una clínica — para fetch() desde la vista Edit</summary>

        [HttpPut]

        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] ClinicaFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

            {

                return BadRequest(new { success = false, message = "El nombre de la clínica es obligatorio." });

            }

            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre}",

                id, dto.Nombre);

            var payload = new

            {

                nombre = dto.Nombre,

                direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion,

                telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono,

                email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email,

                logoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl,

                tiempoEsperaMinutos = dto.TiempoEsperaMinutos > 0 ? dto.TiempoEsperaMinutos : 30,

                bdExterna1 = string.IsNullOrWhiteSpace(dto.BdExterna1) ? null : dto.BdExterna1,

                bdExterna2 = string.IsNullOrWhiteSpace(dto.BdExterna2) ? null : dto.BdExterna2,

                horarioApertura = string.IsNullOrWhiteSpace(dto.HorarioApertura) ? null : dto.HorarioApertura,

                horarioCierre = string.IsNullOrWhiteSpace(dto.HorarioCierre) ? null : dto.HorarioCierre,

                diasAtencion = string.IsNullOrWhiteSpace(dto.DiasAtencion) ? null : dto.DiasAtencion

            };

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Clinicas/{id}", payload);

            if (!success)

            {

                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar clínica" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Clínica actualizada exitosamente" });

        }

        /// <summary>Desactiva una clínica — para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonDesactivar(Guid id)

        {

            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Clinicas/{id}/desactivar", null);

            if (!success)

            {

                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar clínica" });

            }

            return Ok(new { success = true, message = "Clínica desactivada exitosamente" });

        }

        /// <summary>Reactiva una clínica — para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonReactivar(Guid id)

        {

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Clinicas/{id}/reactivar", null);

            if (!success)

            {

                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar clínica" });

            }

            return Ok(new { success = true, message = "Clínica reactivada exitosamente" });

        }

        // ----------------------------------------------------------------
        // LOGO — Subir logo de la clínica (proxy multipart)
        // ----------------------------------------------------------------

        /// <summary>Sube el logo de la clínica — para fetch() desde Create/Edit</summary>

        [HttpPost]

        public async Task<IActionResult> JsonSubirLogo(Guid clinicaId, IFormFile file)

        {

            if (file == null || file.Length == 0)

            {

                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo." });

            }

            _logger.LogInformation("JsonSubirLogo called: clinicaId={ClinicaId}, archivo={Nombre}",

                clinicaId, file.FileName);

            using var stream = file.OpenReadStream();

            var (success, response, errorMessage) = await _apiClient.PostMultipartAsync<JsonElement>(

                $"api/Clinicas/{clinicaId}/logo", file.FileName, stream, file.ContentType, null, "file");

            if (!success)

            {

                _logger.LogWarning("JsonSubirLogo API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al subir el logo" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Logo subido exitosamente" });

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
