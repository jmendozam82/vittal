using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Expedientes.Controllers
{
    /// <summary>
    /// DTO interno para recibir datos del formulario de expedientes desde el cliente.
    /// </summary>
    public class ExpedienteFormDto
    {
        public string PacienteId { get; set; } = string.Empty;
        public string DoctorId { get; set; } = string.Empty;
        public string? NotasGenerales { get; set; }
    }

    /// <summary>
    /// DTO interno para crear una nueva hoja de cita.
    /// </summary>
    public class HojaCitaFormDto
    {
        public string ExpedienteId { get; set; } = string.Empty;
        public string CitaId { get; set; } = string.Empty;
        public string DoctorId { get; set; } = string.Empty;
        public string? FechaConsulta { get; set; }
        public string? MotivoConsulta { get; set; }
        public string? NotasConsulta { get; set; }
    }

    /// <summary>
    /// DTO interno para agregar un diagnóstico a una hoja de cita.
    /// </summary>
    public class DiagnosticoFormDto
    {
        public string HojaCitaId { get; set; } = string.Empty;
        public string DiagnosticoId { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO interno para agregar un tratamiento a una hoja de cita.
    /// </summary>
    public class TratamientoFormDto
    {
        public string HojaCitaId { get; set; } = string.Empty;
        public string? MedicamentoId { get; set; }
        public string? TratamientoId { get; set; }
        public string? Dosis { get; set; }
        public string? Frecuencia { get; set; }
        public string? Duracion { get; set; }
        public string? Instrucciones { get; set; }
    }

    /// <summary>
    /// DTO interno para agregar una cirugía a una hoja de cita.
    /// </summary>
    public class CirugiaFormDto
    {
        public string HojaCitaId { get; set; } = string.Empty;
        public string CirugiaId { get; set; } = string.Empty;
        public string? FechaCirugia { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO interno para agregar un examen a una hoja de cita.
    /// </summary>
    public class ExamenFormDto
    {
        public string HojaCitaId { get; set; } = string.Empty;
        public string ExamenId { get; set; } = string.Empty;
        public string? Resultado { get; set; }
        public string? ArchivoUrl { get; set; }
    }

    /// <summary>
    /// DTO interno para agregar una recomendación a una hoja de cita.
    /// </summary>
    public class RecomendacionFormDto
    {
        public string HojaCitaId { get; set; } = string.Empty;
        public string RecomendacionId { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    [Area("Expedientes")]
    [Authorize]
    public class ExpedientesController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<ExpedientesController> _logger;

        public ExpedientesController(ApiClientHelper apiClient, ILogger<ExpedientesController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ===================== VISTAS =====================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, Guid? citaId = null)
        {
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Expediente no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Expediente = data;
            ViewBag.ExpedienteId = id;
            ViewBag.CitaId = citaId;  // Para auto-seleccionar en el modal de Nueva Hoja de Cita

            // Si viene una citaId, obtener sus datos para el modal
            if (citaId.HasValue)
            {
                var (citaSuccess, citaResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{citaId.Value}");
                if (citaSuccess)
                {
                    ViewBag.CitaData = ExtractDataObject(citaResponse);
                }
            }

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
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Expediente no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Expediente = data;
            return View();
        }

        // ===================== JSON PROXY ENDPOINTS =====================

        /// <summary>Lista todos los expedientes — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonExpedientes()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Expedientes");

            if (!success)
            {
                _logger.LogWarning("JsonExpedientes API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar expedientes" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene expediente por ID — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonExpediente(Guid id)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/{id}");

            if (!success)
            {
                _logger.LogWarning("JsonExpediente API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar expediente" });
            }

            var data = ExtractDataObject(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene hojas de cita de un expediente — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonHojasCita(Guid expedienteId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/HojasCita/expediente/{expedienteId}");

            if (!success)
            {
                _logger.LogWarning("JsonHojasCita API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar hojas de cita" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene diagnósticos de una hoja de cita — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonDiagnosticos(Guid hojaCitaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/hojas-diagnostico/hoja-cita/{hojaCitaId}");

            if (!success)
            {
                _logger.LogWarning("JsonDiagnosticos API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar diagnósticos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene tratamientos de una hoja de cita — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonTratamientos(Guid hojaCitaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/hojas-tratamiento/hoja-cita/{hojaCitaId}");

            if (!success)
            {
                _logger.LogWarning("JsonTratamientos API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar tratamientos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene cirugías de una hoja de cita — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonCirugias(Guid hojaCitaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/hojas-cirugia/hoja-cita/{hojaCitaId}");

            if (!success)
            {
                _logger.LogWarning("JsonCirugias API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar cirugías" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene exámenes de una hoja de cita — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonExamenes(Guid hojaCitaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/hojas-examen/hoja-cita/{hojaCitaId}");

            if (!success)
            {
                _logger.LogWarning("JsonExamenes API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar exámenes" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene archivos de un expediente — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonArchivos(Guid expedienteId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/expedientes-archivos/expediente/{expedienteId}");

            if (!success)
            {
                _logger.LogWarning("JsonArchivos API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar archivos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene pacientes para el dropdown — para fetch() desde Create/Edit</summary>
        [HttpGet]
        public async Task<IActionResult> JsonPacientes()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Pacientes");

            if (!success)
            {
                _logger.LogWarning("JsonPacientes API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar pacientes" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Obtiene doctores para el dropdown — para fetch() desde Create/Edit</summary>
        [HttpGet]
        public async Task<IActionResult> JsonDoctores()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Usuarios");

            if (!success)
            {
                _logger.LogWarning("JsonDoctores API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar doctores" });
            }

            var data = ExtractDataArray(response);
            // Filtrar solo doctores
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

        /// <summary>Catálogo de diagnósticos — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonDiagnosticosCatalogo()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Diagnosticos");
            if (!success)
            {
                _logger.LogWarning("JsonDiagnosticosCatalogo API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Catálogo de medicamentos — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonMedicamentosCatalogo()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Medicamentos");
            if (!success)
            {
                _logger.LogWarning("JsonMedicamentosCatalogo API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Catálogo de tratamientos — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonTratamientosCatalogo()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Tratamientos");
            if (!success)
            {
                _logger.LogWarning("JsonTratamientosCatalogo API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Catálogo de cirugías — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonCirugiasCatalogo()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Cirugias");
            if (!success)
            {
                _logger.LogWarning("JsonCirugiasCatalogo API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Catálogo de exámenes — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonExamenesCatalogo()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Examenes");
            if (!success)
            {
                _logger.LogWarning("JsonExamenesCatalogo API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Busca pacientes por término — para autocomplete en Create</summary>
        [HttpGet]
        public async Task<IActionResult> JsonBuscarPacientes([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = true, data = new List<object>() });
            }

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Pacientes/buscar?q={Uri.EscapeDataString(q)}");

            if (!success)
            {
                _logger.LogWarning("JsonBuscarPacientes API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al buscar pacientes" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        // ===================== ACCIONES CRUD =====================

        /// <summary>Crea un nuevo expediente — para fetch() desde la vista Create</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrear([FromBody] ExpedienteFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PacienteId) || !Guid.TryParse(dto.PacienteId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un paciente." });
            }
            if (string.IsNullOrWhiteSpace(dto.DoctorId) || !Guid.TryParse(dto.DoctorId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un doctor." });
            }

            _logger.LogInformation("JsonCrear expediente: paciente={PacienteId}, doctor={DoctorId}",
                dto.PacienteId, dto.DoctorId);

            var payload = new
            {
                pacienteId = dto.PacienteId,
                doctorId = dto.DoctorId,
                notasGenerales = dto.NotasGenerales
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Expedientes", payload);

            if (!success)
            {
                _logger.LogWarning("JsonCrear expediente API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear expediente" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Expediente creado exitosamente" });
        }

        /// <summary>Actualiza un expediente — para fetch() desde la vista Edit</summary>
        [HttpPut]
        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] ExpedienteFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DoctorId) || !Guid.TryParse(dto.DoctorId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un doctor." });
            }

            _logger.LogInformation("JsonActualizar expediente: id={Id}", id);

            var payload = new
            {
                pacienteId = dto.PacienteId,
                doctorId = dto.DoctorId,
                notasGenerales = dto.NotasGenerales
            };

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Expedientes/{id}", payload);

            if (!success)
            {
                _logger.LogWarning("JsonActualizar expediente API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar expediente" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Expediente actualizado exitosamente" });
        }

        /// <summary>Desactiva un expediente — para fetch() desde la vista Index</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivar(Guid id)
        {
            _logger.LogInformation("JsonDesactivar expediente: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Expedientes/{id}/desactivar", null);

            if (!success)
            {
                _logger.LogWarning("JsonDesactivar expediente API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar expediente" });
            }

            return Ok(new { success = true, message = "Expediente desactivado exitosamente" });
        }

        // ===================== ACCIONES DE HOJAS DE CITA (sub-módulos) =====================

        /// <summary>Crea una nueva hoja de cita — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearHojaCita([FromBody] HojaCitaFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExpedienteId) || !Guid.TryParse(dto.ExpedienteId, out _))
            {
                return BadRequest(new { success = false, message = "Expediente inválido." });
            }

            _logger.LogInformation("JsonCrearHojaCita: expediente={ExpedienteId}", dto.ExpedienteId);

            var payload = new
            {
                expedienteId = dto.ExpedienteId,
                citaId = dto.CitaId,
                doctorId = dto.DoctorId,
                fechaConsulta = dto.FechaConsulta,
                motivoConsulta = dto.MotivoConsulta,
                notasConsulta = dto.NotasConsulta
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/HojasCita", payload);

            if (!success)
            {
                _logger.LogWarning("JsonCrearHojaCita API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear hoja de cita" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Hoja de cita creada exitosamente" });
        }

        /// <summary>Agrega un diagnóstico a una hoja de cita — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearDiagnostico([FromBody] DiagnosticoFormDto dto)
        {
            var payload = new
            {
                hojaCitaId = dto.HojaCitaId,
                diagnosticoId = dto.DiagnosticoId,
                observaciones = dto.Observaciones
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/hojas-diagnostico", payload);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al agregar diagnóstico" });
            }

            return Ok(new { success = true, message = "Diagnóstico agregado exitosamente" });
        }

        /// <summary>Agrega un tratamiento a una hoja de cita — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearTratamiento([FromBody] TratamientoFormDto dto)
        {
            var payload = new
            {
                hojaCitaId = dto.HojaCitaId,
                medicamentoId = string.IsNullOrWhiteSpace(dto.MedicamentoId) ? null : dto.MedicamentoId,
                tratamientoId = string.IsNullOrWhiteSpace(dto.TratamientoId) ? null : dto.TratamientoId,
                dosis = dto.Dosis,
                frecuencia = dto.Frecuencia,
                duracion = dto.Duracion,
                instrucciones = dto.Instrucciones
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/hojas-tratamiento", payload);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al agregar tratamiento" });
            }

            return Ok(new { success = true, message = "Tratamiento agregado exitosamente" });
        }

        /// <summary>Agrega una cirugía a una hoja de cita — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearCirugia([FromBody] CirugiaFormDto dto)
        {
            var payload = new
            {
                hojaCitaId = dto.HojaCitaId,
                cirugiaId = dto.CirugiaId,
                fechaCirugia = dto.FechaCirugia,
                observaciones = dto.Observaciones
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/hojas-cirugia", payload);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al agregar cirugía" });
            }

            return Ok(new { success = true, message = "Cirugía agregada exitosamente" });
        }

        /// <summary>Agrega un examen a una hoja de cita — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearExamen([FromBody] ExamenFormDto dto)
        {
            var payload = new
            {
                hojaCitaId = dto.HojaCitaId,
                examenId = dto.ExamenId,
                resultado = dto.Resultado,
                archivoUrl = dto.ArchivoUrl
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/hojas-examen", payload);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al agregar examen" });
            }

            return Ok(new { success = true, message = "Examen agregado exitosamente" });
        }

        /// <summary>Obtiene recomendaciones de una hoja de cita — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonRecomendaciones(Guid hojaCitaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/hojas-recomendacion/hoja-cita/{hojaCitaId}");

            if (!success)
            {
                _logger.LogWarning("JsonRecomendaciones API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar recomendaciones" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Catálogo de recomendaciones — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonRecomendacionesCatalogo()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Recomendaciones");
            if (!success)
            {
                _logger.LogWarning("JsonRecomendacionesCatalogo API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Agrega una recomendación a una hoja de cita — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrearRecomendacion([FromBody] RecomendacionFormDto dto)
        {
            var payload = new
            {
                hojaCitaId = dto.HojaCitaId,
                recomendacionId = dto.RecomendacionId,
                observaciones = dto.Observaciones
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/hojas-recomendacion", payload);

            if (!success)
            {
                return BadRequest(new { success = false, message = errorMessage ?? "Error al agregar recomendación" });
            }

            return Ok(new { success = true, message = "Recomendación agregada exitosamente" });
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
