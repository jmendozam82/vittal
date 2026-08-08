using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Catalogos.Controllers

{

    /// <summary>

    /// DTO interno para recibir datos del formulario de pacientes desde el cliente.

    /// </summary>

    public class PacienteFormDto

    {

        public string DoctorId { get; set; } = string.Empty;

        public string PrimerNombre { get; set; } = string.Empty;

        public string? SegundoNombre { get; set; }

        public string PrimerApellido { get; set; } = string.Empty;

        public string? SegundoApellido { get; set; }

        public string? Email { get; set; }

        public string? Celular { get; set; }

        public string? Direccion { get; set; }

        public string? Sexo { get; set; }

        public string? FechaNacimiento { get; set; }

        public string? FotoUrl { get; set; }

        public string? Observaciones { get; set; }

        public string? TipoDocumentoIdentificacion { get; set; }

        public string? NumeroDocumentoIdentificacion { get; set; }

    }

    [Area("Catalogos")]

    [Authorize]

    public class PacienteController : Controller

    {

        private readonly ApiClientHelper _apiClient;

        private readonly ILogger<PacienteController> _logger;

        public PacienteController(ApiClientHelper apiClient, ILogger<PacienteController> logger)

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

            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Pacientes/{id}");

            if (!success)

            {

                TempData["Error"] = "Error al conectar con la API.";

                return RedirectToAction("Index");

            }

            var data = ExtractDataObject(response);

            if (data == null)

            {

                TempData["Error"] = "Paciente no encontrado.";

                return RedirectToAction("Index");

            }

            ViewBag.Paciente = data;

            return View();

        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todos los pacientes � para fetch() desde la vista Index</summary>

        [HttpGet]

        public async Task<IActionResult> JsonPacientes([FromQuery] bool inactivos = false)

        {

            var url = inactivos ? "api/Pacientes?inactivos=true" : "api/Pacientes";

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)

            {

                _logger.LogWarning("JsonPacientes API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar pacientes" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Busca pacientes por t�rmino � para fetch() desde la vista Index</summary>

        [HttpGet]

        public async Task<IActionResult> JsonBuscar([FromQuery] string q)

        {

            if (string.IsNullOrWhiteSpace(q))

            {

                return Json(new { success = true, data = new List<object>() });

            }

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Pacientes/buscar?q={Uri.EscapeDataString(q)}");

            if (!success)

            {

                _logger.LogWarning("JsonBuscar API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al buscar pacientes" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Obtiene doctores para el dropdown � para fetch() desde Create/Edit</summary>

        /// <summary>Obtiene doctores para el dropdown — para fetch() desde Create/Edit.
        /// Usa api/agenda/catalogos (permiso "agenda") en lugar de api/Usuarios
        /// (permiso "usuarios"), para que la Recepcionista pueda asignar doctor
        /// sin requerir permiso de administración de usuarios (Hallazgo QA #2).
        /// </summary>

        [HttpGet]

        public async Task<IActionResult> JsonDoctores()

        {

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/agenda/catalogos");

            if (!success)

            {

                _logger.LogWarning("JsonDoctores API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar doctores" });

            }

            var data = ExtractCatalogosDataArray(response, "doctores");

            // Filtrar solo doctores (defensa en profundidad: el endpoint ya filtra)

            var doctores = new List<object>();

            foreach (var d in data)

            {

                var dict = d as Dictionary<string, object?>;

                if (dict != null)

                {

                    var esDoctor = dict.TryGetValue("esDoctor", out var val) && val is bool b && b;

                    if (esDoctor)

                    {

                        doctores.Add(dict);

                    }

                }

            }

            return Json(new { success = true, data = doctores });

        }

        /// <summary>Crea un nuevo paciente � para fetch() desde la vista Create</summary>

        [HttpPost]

        public async Task<IActionResult> JsonCrear([FromBody] PacienteFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.PrimerNombre))

            {

                return BadRequest(new { success = false, message = "El primer nombre es obligatorio." });

            }

            if (string.IsNullOrWhiteSpace(dto.PrimerApellido))

            {

                return BadRequest(new { success = false, message = "El primer apellido es obligatorio." });

            }

            if (string.IsNullOrWhiteSpace(dto.DoctorId) || !Guid.TryParse(dto.DoctorId, out _))

            {

                return BadRequest(new { success = false, message = "Debe seleccionar un doctor." });

            }

            if (string.IsNullOrWhiteSpace(dto.TipoDocumentoIdentificacion) || !new[] { "CC", "CR", "PA" }.Contains(dto.TipoDocumentoIdentificacion))

            {

                return BadRequest(new { success = false, message = "El tipo de documento debe ser CC, CR o PA" });

            }

            if (string.IsNullOrWhiteSpace(dto.NumeroDocumentoIdentificacion) || dto.NumeroDocumentoIdentificacion.Length < 5)

            {

                return BadRequest(new { success = false, message = "El n�mero de documento es obligatorio y debe tener al menos 5 caracteres" });

            }

            _logger.LogInformation("JsonCrear called: nombre={Nombre} {Apellido}",

                dto.PrimerNombre, dto.PrimerApellido);

            var payload = new

            {

                doctorId = dto.DoctorId,

                primerNombre = dto.PrimerNombre,

                segundoNombre = dto.SegundoNombre,

                primerApellido = dto.PrimerApellido,

                segundoApellido = dto.SegundoApellido,

                email = dto.Email,

                celular = dto.Celular,

                direccion = dto.Direccion,

                sexo = dto.Sexo,

                fechaNacimiento = !string.IsNullOrWhiteSpace(dto.FechaNacimiento) ? dto.FechaNacimiento : null,

                fotoUrl = dto.FotoUrl,

                observaciones = dto.Observaciones,

                tipoDocumentoIdentificacion = dto.TipoDocumentoIdentificacion,

                numeroDocumentoIdentificacion = dto.NumeroDocumentoIdentificacion

            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Pacientes", payload);

            if (!success)

            {

                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear paciente" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Paciente creado exitosamente" });

        }

        /// <summary>Actualiza un paciente � para fetch() desde la vista Edit</summary>

        [HttpPut]

        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] PacienteFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.PrimerNombre))

            {

                return BadRequest(new { success = false, message = "El primer nombre es obligatorio." });

            }

            if (string.IsNullOrWhiteSpace(dto.PrimerApellido))

            {

                return BadRequest(new { success = false, message = "El primer apellido es obligatorio." });

            }

            if (string.IsNullOrWhiteSpace(dto.TipoDocumentoIdentificacion) || !new[] { "CC", "CR", "PA" }.Contains(dto.TipoDocumentoIdentificacion))

            {

                return BadRequest(new { success = false, message = "El tipo de documento debe ser CC, CR o PA" });

            }

            if (string.IsNullOrWhiteSpace(dto.NumeroDocumentoIdentificacion) || dto.NumeroDocumentoIdentificacion.Length < 5)

            {

                return BadRequest(new { success = false, message = "El n�mero de documento es obligatorio y debe tener al menos 5 caracteres" });

            }

            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre} {Apellido}",

                id, dto.PrimerNombre, dto.PrimerApellido);

            var payload = new

            {

                doctorId = dto.DoctorId,

                primerNombre = dto.PrimerNombre,

                segundoNombre = dto.SegundoNombre,

                primerApellido = dto.PrimerApellido,

                segundoApellido = dto.SegundoApellido,

                email = dto.Email,

                celular = dto.Celular,

                direccion = dto.Direccion,

                sexo = dto.Sexo,

                fechaNacimiento = !string.IsNullOrWhiteSpace(dto.FechaNacimiento) ? dto.FechaNacimiento : null,

                fotoUrl = dto.FotoUrl,

                observaciones = dto.Observaciones,

                tipoDocumentoIdentificacion = dto.TipoDocumentoIdentificacion,

                numeroDocumentoIdentificacion = dto.NumeroDocumentoIdentificacion

            };

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Pacientes/{id}", payload);

            if (!success)

            {

                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar paciente" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Paciente actualizado exitosamente" });

        }

        /// <summary>Desactiva un paciente � para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonDesactivar(Guid id)

        {

            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Pacientes/{id}/desactivar", null);

            if (!success)

            {

                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar paciente" });

            }

            return Ok(new { success = true, message = "Paciente desactivado exitosamente" });

        }

        /// <summary>Reactiva un paciente � para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonReactivar(Guid id)

        {

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Pacientes/{id}/reactivar", null);

            if (!success)

            {

                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar paciente" });

            }

            return Ok(new { success = true, message = "Paciente reactivado exitosamente" });

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

        /// <summary>

        /// Extrae un arreglo de una propiedad interna del objeto "data" de la respuesta.

        /// Se usa para los catálogos agregados (ej: api/agenda/catalogos → data.doctores).

        /// </summary>

        private static List<object> ExtractCatalogosDataArray(JsonElement? response, string property)

        {

            if (!response.HasValue) return new List<object>();

            try

            {

                if (response.Value.TryGetProperty("data", out var dataProp) &&

                    dataProp.ValueKind == JsonValueKind.Object &&

                    dataProp.TryGetProperty(property, out var arr) &&

                    arr.ValueKind == JsonValueKind.Array)

                {

                    return EnumerateJsonArray(arr).ToList();

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
