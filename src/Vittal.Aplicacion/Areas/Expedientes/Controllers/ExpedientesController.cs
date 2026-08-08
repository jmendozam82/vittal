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

    /// <summary>
    /// DTO interno para guardar signos vitales en una hoja de cita.
    /// El salaId se resuelve automáticamente desde la cita de la hoja.
    /// </summary>
    public class SignosVitalesHojaFormDto
    {
        public string HojaCitaId { get; set; } = string.Empty;
        public List<SignoVitalItemFormDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO interno para un registro de signo vital.
    /// </summary>
    public class SignoVitalItemFormDto
    {
        public string TipoSignoVitalId { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string? Unidad { get; set; }
    }

    /// <summary>
    /// DTO interno para guardar antecedentes del paciente por sala (upsert).
    /// </summary>
    public class AntecedentesFormDto
    {
        public string ExpedienteId { get; set; } = string.Empty;
        public string SalaId { get; set; } = string.Empty;
        public List<AntecedenteItemFormDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO interno para un antecedente del paciente.
    /// Valor en texto: 'true'/'false' para boolean, numérico como string, o texto libre.
    /// </summary>
    public class AntecedenteItemFormDto
    {
        public string TipoAntecedenteId { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel para la vista de impresión de receta médica.
    /// </summary>
    public class RecetaMedicaViewModel
    {
        public string ClinicaNombre { get; set; } = "Clínica Vittal";
        public string ClinicaDireccion { get; set; } = string.Empty;
        public string ClinicaTelefono { get; set; } = string.Empty;
        public string ClinicaEmail { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string DoctorNombre { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        public string? PacienteTipoDocumento { get; set; }
        public string? PacienteDocumento { get; set; }
        public string? PacienteEmail { get; set; }
        public string? PacienteCelular { get; set; }
        public string? PacienteDireccion { get; set; }
        public string? PacienteEdad { get; set; }
        public string? PacienteSexo { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public List<MedicamentoReceta> Medicamentos { get; set; } = new();
        public List<TratamientoReceta> Tratamientos { get; set; } = new();
    }

    public class MedicamentoReceta
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Dosis { get; set; }
        public string? Frecuencia { get; set; }
        public string? Duracion { get; set; }
        public string? Instrucciones { get; set; }
    }

    public class TratamientoReceta
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Instrucciones { get; set; }
    }

    // ── ViewModels para Epicrisis ──────────────────────────────────

    public class EpicrisisViewModel
    {
        public string ClinicaNombre { get; set; } = "Clínica Vittal";
        public string ClinicaDireccion { get; set; } = string.Empty;
        public string ClinicaTelefono { get; set; } = string.Empty;
        public string ClinicaEmail { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string DoctorNombre { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        public string? PacienteTipoDocumento { get; set; }
        public string? PacienteDocumento { get; set; }
        public string? PacienteEdad { get; set; }
        public string? PacienteSexo { get; set; }
        public string? PacienteEmail { get; set; }
        public string? PacienteCelular { get; set; }
        public string FechaConsulta { get; set; } = string.Empty;
        public string? MotivoConsulta { get; set; }
        public string? NotasConsulta { get; set; }
        public List<DiagnosticoEpicrisis> Diagnosticos { get; set; } = new();
        public List<AntecedenteEpicrisis> Antecedentes { get; set; } = new();
        public List<SignoVitalEpicrisis> SignosVitales { get; set; } = new();
        public List<string> Tratamientos { get; set; } = new();
        public List<string> Cirugias { get; set; } = new();
        public List<string> Examenes { get; set; } = new();
        public List<string> Recomendaciones { get; set; } = new();
    }

    public class AntecedenteEpicrisis
    {
        public string Nombre { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
    }

    public class SignoVitalEpicrisis
    {
        public string Nombre { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public bool FueraDeRango { get; set; }
    }

    public class DiagnosticoEpicrisis
    {
        public string TipoNombre { get; set; } = string.Empty;
        public string? DiagnosticoNombre { get; set; }
        public string? Observaciones { get; set; }
        public bool EsPrincipal { get; set; }
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

        /// <summary>Obtiene una cita por su ID — proxy MVC para el prefill del modal de hoja de cita.
        /// El médico puede no tener permiso sobre el listado de usuarios (api/Usuarios → 403),
        /// pero la cita trae doctorId y doctorNombre para precargar el modal.</summary>
        [HttpGet]
        public async Task<IActionResult> JsonCita(Guid id)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{id}");

            if (!success)
            {
                _logger.LogWarning("JsonCita API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar la cita" });
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

        /// <summary>Obtiene archivos de un expediente o de una hoja de cita — para fetch() desde Details</summary>
        [HttpGet]
        public async Task<IActionResult> JsonArchivos(Guid? expedienteId = null, Guid? hojaCitaId = null)
        {
            string apiEndpoint;
            if (hojaCitaId.HasValue)
            {
                apiEndpoint = $"api/expedientes-archivos/hoja-cita/{hojaCitaId}";
            }
            else if (expedienteId.HasValue)
            {
                apiEndpoint = $"api/expedientes-archivos/expediente/{expedienteId}";
            }
            else
            {
                return Json(new { success = false, message = "Se requiere expedienteId o hojaCitaId." });
            }

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(apiEndpoint);

            if (!success)
            {
                _logger.LogWarning("JsonArchivos API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar archivos" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Proxy para subir archivos: recibe multipart/form-data y reenvía al API</summary>
        [HttpPost]
        public async Task<IActionResult> JsonSubirArchivo(IFormFile file, Guid expedienteId, Guid? hojaCitaId = null)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Debe seleccionar un archivo." });
            }

            var fields = new Dictionary<string, string>
            {
                { "expedienteId", expedienteId.ToString() }
            };
            if (hojaCitaId.HasValue)
            {
                fields["hojaCitaId"] = hojaCitaId.Value.ToString();
            }

            using var stream = file.OpenReadStream();
            var (success, data, errorMessage) = await _apiClient.PostMultipartAsync<JsonElement>(
                "api/expedientes-archivos/upload",
                file.FileName,
                stream,
                file.ContentType,
                fields);

            if (!success)
            {
                _logger.LogWarning("JsonSubirArchivo API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al subir archivo" });
            }

            return Json(new { success = true, message = "Archivo subido exitosamente.", data });
        }

        /// <summary>Proxy para descargar archivo (URL firmada) desde el API</summary>
        [HttpGet]
        public async Task<IActionResult> JsonSignedUrl(Guid id)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(
                $"api/expedientes-archivos/{id}/signed-url");

            if (!success)
            {
                _logger.LogWarning("JsonSignedUrl API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al obtener URL" });
            }

            return Json(new { success = true, data = response.TryGetProperty("data", out var d) ? d.GetString() : "" });
        }

        /// <summary>Proxy para eliminar archivo desde el API</summary>
        [HttpPost]
        public async Task<IActionResult> JsonEliminarArchivo(Guid id)
        {
            var (success, response, errorMessage) = await _apiClient.PatchAsync<JsonElement>(
                $"api/expedientes-archivos/{id}/desactivar", new { });

            if (!success)
            {
                _logger.LogWarning("JsonEliminarArchivo API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al eliminar archivo" });
            }

            return Json(new { success = true, message = "Archivo eliminado exitosamente." });
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

        /// <summary>
        /// Catálogo unificado de expedientes — proxy con JWT.
        /// Llama a api/expedientes/catalogos (requiere solo permiso expedientes:Read,
        /// que el Doctor conserva) y devuelve en una sola respuesta los catálogos
        /// de la hoja de cita: diagnosticos, medicamentos, tratamientos, cirugias,
        /// examenes, recomendaciones, tiposSignoVital y tiposAntecedente.
        /// Patrón establecido en el hallazgo #26.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> JsonExpedientesCatalogos()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/expedientes/catalogos");
            if (!success)
            {
                _logger.LogWarning("JsonExpedientesCatalogos API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataObject(response) });
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

        // ===================== SIGNOS VITALES (HU-E06) =====================

        /// <summary>Obtiene la sala de una hoja de cita (vía su cita) — para cargar catálogos por sala.</summary>
        [HttpGet]
        public async Task<IActionResult> JsonSalaDeHoja(Guid hojaCitaId)
        {
            var (ok, salaId, salaNombre) = await ObtenerSalaDeHojaAsync(hojaCitaId);
            if (!ok)
            {
                return Json(new { success = false, message = "No se pudo determinar la sala de la consulta." });
            }
            return Json(new { success = true, salaId = salaId, salaNombre = salaNombre });
        }

        /// <summary>Catálogo de tipos de signos vitales de una sala — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonTiposSignoVital(Guid salaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/TipoSignoVital/sala/{salaId}");
            if (!success)
            {
                _logger.LogWarning("JsonTiposSignoVital API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Obtiene los signos vitales registrados en una hoja de cita (histórico de la consulta).</summary>
        [HttpGet]
        public async Task<IActionResult> JsonSignosVitalesHoja(Guid hojaCitaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/SignosVitalesHoja/hoja/{hojaCitaId}");
            if (!success)
            {
                _logger.LogWarning("JsonSignosVitalesHoja API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar signos vitales" });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Guarda signos vitales en una hoja de cita — el salaId se resuelve desde la cita de la hoja.</summary>
        [HttpPost]
        public async Task<IActionResult> JsonGuardarSignosVitales([FromBody] SignosVitalesHojaFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.HojaCitaId) || !Guid.TryParse(dto.HojaCitaId, out var hojaCitaId))
            {
                return BadRequest(new { success = false, message = "Hoja de cita inválida." });
            }

            var (ok, salaId, _) = await ObtenerSalaDeHojaAsync(hojaCitaId);
            if (!ok)
            {
                return BadRequest(new { success = false, message = "No se pudo determinar la sala de la consulta." });
            }

            _logger.LogInformation("JsonGuardarSignosVitales: hoja={HojaCitaId}, items={Count}", hojaCitaId, dto.Items.Count);

            var guardados = 0;
            var errores = new List<string>();
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.TipoSignoVitalId) || !Guid.TryParse(item.TipoSignoVitalId, out var tipoId))
                {
                    continue;
                }

                var payload = new
                {
                    hojaCitaId = hojaCitaId,
                    salaId = salaId,
                    tipoSignoVitalId = tipoId,
                    valor = item.Valor,
                    unidad = string.IsNullOrWhiteSpace(item.Unidad) ? null : item.Unidad
                };

                var (success, _, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/SignosVitalesHoja", payload);
                if (success)
                {
                    guardados++;
                }
                else
                {
                    errores.Add(errorMessage ?? "Error desconocido");
                }
            }

            if (guardados == 0 && errores.Count > 0)
            {
                _logger.LogWarning("JsonGuardarSignosVitales: no se guardó ningún ítem. Errores: {Errors}", string.Join("; ", errores));
                return BadRequest(new { success = false, message = "No se pudo guardar ningún signo vital. " + string.Join(" ", errores.Take(2)) });
            }

            return Ok(new { success = true, message = $"Signos vitales guardados ({guardados}).", guardados = guardados });
        }

        // ===================== ANTECEDENTES DEL PACIENTE (HU-E05) =====================

        /// <summary>Catálogo de tipos de antecedentes de una sala — proxy con JWT</summary>
        [HttpGet]
        public async Task<IActionResult> JsonTiposAntecedente(Guid salaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/TipoAntecedente/sala/{salaId}");
            if (!success)
            {
                _logger.LogWarning("JsonTiposAntecedente API call failed: {Error}", errorMessage);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Obtiene los antecedentes existentes de un paciente en una sala (para pre-cargar en el modal).</summary>
        [HttpGet]
        public async Task<IActionResult> JsonAntecedentes(Guid expedienteId, Guid salaId)
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/AntecedentesPaciente/expediente/{expedienteId}/sala/{salaId}");
            if (!success)
            {
                _logger.LogWarning("JsonAntecedentes API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar antecedentes" });
            }
            return Json(new { success = true, data = ExtractDataArray(response) });
        }

        /// <summary>Guarda antecedentes del paciente por sala (upsert) — para fetch() desde Details</summary>
        [HttpPost]
        public async Task<IActionResult> JsonGuardarAntecedentes([FromBody] AntecedentesFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExpedienteId) || !Guid.TryParse(dto.ExpedienteId, out var expedienteId))
            {
                return BadRequest(new { success = false, message = "Expediente inválido." });
            }
            if (string.IsNullOrWhiteSpace(dto.SalaId) || !Guid.TryParse(dto.SalaId, out var salaId))
            {
                return BadRequest(new { success = false, message = "Sala inválida." });
            }

            _logger.LogInformation("JsonGuardarAntecedentes: expediente={ExpedienteId}, sala={SalaId}, items={Count}",
                expedienteId, salaId, dto.Items.Count);

            var guardados = 0;
            var errores = new List<string>();
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.TipoAntecedenteId) || !Guid.TryParse(item.TipoAntecedenteId, out var tipoId))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.Valor))
                {
                    continue;
                }

                var payload = new
                {
                    expedienteId = expedienteId,
                    salaId = salaId,
                    tipoAntecedenteId = tipoId,
                    valor = item.Valor
                };

                var (success, _, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/AntecedentesPaciente", payload);
                if (success)
                {
                    guardados++;
                }
                else
                {
                    errores.Add(errorMessage ?? "Error desconocido");
                }
            }

            if (guardados == 0 && errores.Count > 0)
            {
                _logger.LogWarning("JsonGuardarAntecedentes: no se guardó ningún ítem. Errores: {Errors}", string.Join("; ", errores));
                return BadRequest(new { success = false, message = "No se pudo guardar ningún antecedente. " + string.Join(" ", errores.Take(2)) });
            }

            return Ok(new { success = true, message = $"Antecedentes guardados ({guardados}).", guardados = guardados });
        }

        /// <summary>Resuelve la sala de una hoja de cita vía su cita asociada.</summary>
        private async Task<(bool Ok, Guid SalaId, string? SalaNombre)> ObtenerSalaDeHojaAsync(Guid hojaCitaId)
        {
            var (hcSuccess, hcResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/HojasCita/{hojaCitaId}");
            if (!hcSuccess) return (false, Guid.Empty, null);

            var hc = ExtractDataObject(hcResponse) as Dictionary<string, object?>;
            var citaId = hc?.GetValueOrDefault("citaId")?.ToString();
            if (string.IsNullOrWhiteSpace(citaId) || !Guid.TryParse(citaId, out var citaGuid))
                return (false, Guid.Empty, null);

            var (citaSuccess, citaResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{citaGuid}");
            if (!citaSuccess) return (false, Guid.Empty, null);

            var cita = ExtractDataObject(citaResponse) as Dictionary<string, object?>;
            var salaId = cita?.GetValueOrDefault("salaId")?.ToString();
            if (string.IsNullOrWhiteSpace(salaId) || !Guid.TryParse(salaId, out var salaGuid))
                return (false, Guid.Empty, null);

            var salaNombre = cita?.GetValueOrDefault("salaNombre") as string;
            return (true, salaGuid, salaNombre);
        }

        // ===================== IMPRIMIR RECETA MÉDICA =====================

        /// <summary>
        /// Muestra la vista de impresión de receta médica para una hoja de cita.
        /// Renderiza una página optimizada con @media print para imprimir.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ImprimirReceta(Guid hojaCitaId)
        {
            var model = new RecetaMedicaViewModel();

            // 1. Obtener hoja de cita
            var (hcSuccess, hcResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/HojasCita/{hojaCitaId}");
            if (!hcSuccess)
            {
                TempData["Error"] = "No se encontró la hoja de cita.";
                return RedirectToAction("Index");
            }

            var hc = ExtractDataObject(hcResponse) as Dictionary<string, object?>;
            if (hc == null)
            {
                TempData["Error"] = "No se encontró la hoja de cita.";
                return RedirectToAction("Index");
            }

            model.DoctorNombre = hc.GetValueOrDefault("doctorNombre") as string ?? "";
            model.PacienteNombre = hc.GetValueOrDefault("pacienteNombre") as string ?? "";
            model.Fecha = hc.GetValueOrDefault("fechaConsulta") as string ?? "";
            model.Motivo = hc.GetValueOrDefault("motivoConsulta") as string;
            model.NumeroDocumento = "REC-" + hojaCitaId.ToString("N").Substring(0, 8).ToUpperInvariant();

            var expedienteId = hc.GetValueOrDefault("expedienteId")?.ToString();

            // 2. Obtener datos del paciente vía el expediente (módulo 'expedientes' — accesible para el Doctor)
            if (!string.IsNullOrWhiteSpace(expedienteId) && Guid.TryParse(expedienteId, out var expId))
            {
                var (pacInfoSuccess, pacInfoResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/{expId}/paciente-info");
                if (pacInfoSuccess)
                {
                    var pac = ExtractDataObject(pacInfoResponse) as Dictionary<string, object?>;
                    model.PacienteTipoDocumento = pac?.GetValueOrDefault("tipoDocumentoIdentificacion") as string;
                    model.PacienteDocumento = pac?.GetValueOrDefault("numeroDocumentoIdentificacion") as string;
                    model.PacienteEmail = pac?.GetValueOrDefault("email") as string;
                    model.PacienteCelular = pac?.GetValueOrDefault("celular") as string;
                    model.PacienteDireccion = pac?.GetValueOrDefault("direccion") as string;
                    model.PacienteSexo = pac?.GetValueOrDefault("sexo") as string;

                    var fechaNacRaw = pac?.GetValueOrDefault("fechaNacimiento") as string;
                    if (!string.IsNullOrWhiteSpace(fechaNacRaw) && DateTime.TryParse(fechaNacRaw, out var fechaNac))
                    {
                        var edad = DateTime.Today.Year - fechaNac.Year;
                        if (DateTime.Today < fechaNac.AddYears(edad)) edad--;
                        model.PacienteEdad = $"{edad} años";
                    }
                }
            }

            // 4. Obtener tratamientos y medicamentos de la hoja de cita
            var (trSuccess, trResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/hojas-tratamiento/hoja-cita/{hojaCitaId}");
            if (trSuccess)
            {
                var items = ExtractDataArray(trResponse);
                foreach (var item in items)
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;

                    var medicamentoNombre = dict.GetValueOrDefault("medicamentoNombre") as string;
                    var tratamientoNombre = dict.GetValueOrDefault("tratamientoNombre") as string;

                    if (!string.IsNullOrWhiteSpace(medicamentoNombre))
                    {
                        model.Medicamentos.Add(new MedicamentoReceta
                        {
                            Nombre = medicamentoNombre,
                            Dosis = dict.GetValueOrDefault("dosis") as string,
                            Frecuencia = dict.GetValueOrDefault("frecuencia") as string,
                            Duracion = dict.GetValueOrDefault("duracion") as string,
                            Instrucciones = dict.GetValueOrDefault("instrucciones") as string
                        });
                    }
                    else if (!string.IsNullOrWhiteSpace(tratamientoNombre))
                    {
                        model.Tratamientos.Add(new TratamientoReceta
                        {
                            Nombre = tratamientoNombre,
                            Instrucciones = dict.GetValueOrDefault("instrucciones") as string
                        });
                    }
                }
            }

            // 5. Obtener datos de la clínica actual (multi-tenant) para encabezado/pie del documento
            await CargarClinicaEnViewModelAsync(model);

            return View(model);
        }

        /// <summary>Carga los datos de la clínica actual del JWT (nombre, logo, dirección, contacto) en un ViewModel de impresión.</summary>
        private async Task CargarClinicaEnViewModelAsync<T>(T model) where T : class
        {
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>("api/Clinicas/current-info");
            if (!success) return;

            var data = ExtractDataObject(response) as Dictionary<string, object?>;
            if (data == null) return;

            // Mapeo: propiedad del ViewModel → campo JSON de ClinicaResponseDto
            var mapeo = new Dictionary<string, string>
            {
                [nameof(RecetaMedicaViewModel.ClinicaNombre)] = "nombre",
                [nameof(RecetaMedicaViewModel.ClinicaDireccion)] = "direccion",
                [nameof(RecetaMedicaViewModel.ClinicaTelefono)] = "telefono",
                [nameof(RecetaMedicaViewModel.ClinicaEmail)] = "email",
                [nameof(RecetaMedicaViewModel.LogoUrl)] = "logoUrl"
            };

            foreach (var (propiedad, campoJson) in mapeo)
            {
                var pi = model.GetType().GetProperty(propiedad);
                if (pi == null || !pi.CanWrite) continue;

                var raw = data.GetValueOrDefault(campoJson) as string;
                if (!string.IsNullOrWhiteSpace(raw))
                    pi.SetValue(model, raw);
            }
        }

        // ===================== IMPRIMIR EPICRISIS =====================

        /// <summary>
        /// Muestra la vista de impresión de epicrisis (resumen de alta) para una hoja de cita.
        /// Ensambla datos de: hoja de cita, diagnósticos, tratamientos, cirugías, exámenes y recomendaciones.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ImprimirEpicrisis(Guid hojaCitaId)
        {
            var model = new EpicrisisViewModel();

            // 1. Obtener hoja de cita
            var (hcSuccess, hcResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/HojasCita/{hojaCitaId}");
            if (!hcSuccess)
            {
                TempData["Error"] = "No se encontró la hoja de cita.";
                return RedirectToAction("Index");
            }

            var hc = ExtractDataObject(hcResponse) as Dictionary<string, object?>;
            if (hc == null)
            {
                TempData["Error"] = "No se encontró la hoja de cita.";
                return RedirectToAction("Index");
            }

            model.DoctorNombre = hc.GetValueOrDefault("doctorNombre") as string ?? "";
            model.PacienteNombre = hc.GetValueOrDefault("pacienteNombre") as string ?? "";
            model.FechaConsulta = hc.GetValueOrDefault("fechaConsulta") as string ?? "";
            model.MotivoConsulta = hc.GetValueOrDefault("motivoConsulta") as string;
            model.NotasConsulta = hc.GetValueOrDefault("notasConsulta") as string;
            model.NumeroDocumento = "EPI-" + hojaCitaId.ToString("N").Substring(0, 8).ToUpperInvariant();

            var expedienteId = hc.GetValueOrDefault("expedienteId")?.ToString();

            // 2. Obtener datos del paciente vía el expediente (módulo 'expedientes' — accesible para el Doctor)
            if (!string.IsNullOrWhiteSpace(expedienteId) && Guid.TryParse(expedienteId, out var expId))
            {
                var (pacInfoSuccess, pacInfoResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/{expId}/paciente-info");
                if (pacInfoSuccess)
                {
                    var pac = ExtractDataObject(pacInfoResponse) as Dictionary<string, object?>;
                    model.PacienteTipoDocumento = pac?.GetValueOrDefault("tipoDocumentoIdentificacion") as string;
                    model.PacienteDocumento = pac?.GetValueOrDefault("numeroDocumentoIdentificacion") as string;
                    model.PacienteSexo = pac?.GetValueOrDefault("sexo") as string;
                    model.PacienteEmail = pac?.GetValueOrDefault("email") as string;
                    model.PacienteCelular = pac?.GetValueOrDefault("celular") as string;

                    var fechaNacRaw = pac?.GetValueOrDefault("fechaNacimiento") as string;
                    if (!string.IsNullOrWhiteSpace(fechaNacRaw) && DateTime.TryParse(fechaNacRaw, out var fechaNac))
                    {
                        var edad = DateTime.Today.Year - fechaNac.Year;
                        if (DateTime.Today < fechaNac.AddYears(edad)) edad--;
                        model.PacienteEdad = $"{edad} años";
                    }
                }
            }

            // 4. Obtener diagnósticos
            var (dxSuccess, dxResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/hojas-diagnostico/hoja-cita/{hojaCitaId}");
            if (dxSuccess)
            {
                foreach (var item in ExtractDataArray(dxResponse))
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;
                    model.Diagnosticos.Add(new DiagnosticoEpicrisis
                    {
                        TipoNombre = dict.GetValueOrDefault("tipoDiagnosticoNombre") as string ?? "",
                        DiagnosticoNombre = dict.GetValueOrDefault("diagnosticoNombre") as string,
                        Observaciones = dict.GetValueOrDefault("observaciones") as string,
                        EsPrincipal = dict.GetValueOrDefault("esPrincipal") is bool b && b
                    });
                }
            }

            // 5. Obtener tratamientos
            var (trSuccess, trResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/hojas-tratamiento/hoja-cita/{hojaCitaId}");
            if (trSuccess)
            {
                foreach (var item in ExtractDataArray(trResponse))
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;
                    var nombre = dict.GetValueOrDefault("tratamientoNombre") as string ?? dict.GetValueOrDefault("medicamentoNombre") as string ?? "";
                    var instrucciones = dict.GetValueOrDefault("instrucciones") as string;
                    if (!string.IsNullOrWhiteSpace(nombre))
                        model.Tratamientos.Add(string.IsNullOrWhiteSpace(instrucciones) ? nombre : $"{nombre} — {instrucciones}");
                }
            }

            // 6. Obtener cirugías
            var (ciSuccess, ciResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/hojas-cirugia/hoja-cita/{hojaCitaId}");
            if (ciSuccess)
            {
                foreach (var item in ExtractDataArray(ciResponse))
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;
                    var nombre = dict.GetValueOrDefault("cirugiaNombre") as string ?? "";
                    if (!string.IsNullOrWhiteSpace(nombre))
                        model.Cirugias.Add(nombre);
                }
            }

            // 7. Obtener exámenes
            var (exSuccess, exResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/hojas-examen/hoja-cita/{hojaCitaId}");
            if (exSuccess)
            {
                foreach (var item in ExtractDataArray(exResponse))
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;
                    var nombre = dict.GetValueOrDefault("examenNombre") as string ?? "";
                    var resultado = dict.GetValueOrDefault("resultado") as string;
                    if (!string.IsNullOrWhiteSpace(nombre))
                        model.Examenes.Add(string.IsNullOrWhiteSpace(resultado) ? nombre : $"{nombre}: {resultado}");
                }
            }

            // 8. Obtener recomendaciones
            var (rcSuccess, rcResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/hojas-recomendacion/hoja-cita/{hojaCitaId}");
            if (rcSuccess)
            {
                foreach (var item in ExtractDataArray(rcResponse))
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;
                    var nombre = dict.GetValueOrDefault("recomendacionNombre") as string ?? "";
                    if (!string.IsNullOrWhiteSpace(nombre))
                        model.Recomendaciones.Add(nombre);
                }
            }

            // 9. Obtener signos vitales de la hoja de cita
            var (svSuccess, svResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/SignosVitalesHoja/hoja/{hojaCitaId}");
            if (svSuccess)
            {
                foreach (var item in ExtractDataArray(svResponse))
                {
                    var dict = item as Dictionary<string, object?>;
                    if (dict == null) continue;
                    var nombre = dict.GetValueOrDefault("tipoSignoVitalNombre") as string ?? "";
                    if (string.IsNullOrWhiteSpace(nombre)) continue;

                    var valor = dict.GetValueOrDefault("valor") is double dv ? dv.ToString("0.##") : "—";
                    model.SignosVitales.Add(new SignoVitalEpicrisis
                    {
                        Nombre = nombre,
                        Valor = valor,
                        Unidad = dict.GetValueOrDefault("unidad") as string ?? "",
                        FueraDeRango = dict.GetValueOrDefault("fueraDeRango") is bool fdr && fdr
                    });
                }
            }

            // 10. Obtener antecedentes del paciente (expediente × sala de la hoja)
            var (salaOk, salaId, _) = await ObtenerSalaDeHojaAsync(hojaCitaId);
            if (salaOk && !string.IsNullOrWhiteSpace(expedienteId) && Guid.TryParse(expedienteId, out var antExpId))
            {
                var (antSuccess, antResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/AntecedentesPaciente/expediente/{antExpId}/sala/{salaId}");
                if (antSuccess)
                {
                    foreach (var item in ExtractDataArray(antResponse))
                    {
                        var dict = item as Dictionary<string, object?>;
                        if (dict == null) continue;
                        var nombre = dict.GetValueOrDefault("tipoAntecedenteNombre") as string ?? "";
                        if (string.IsNullOrWhiteSpace(nombre)) continue;

                        var tipoDato = dict.GetValueOrDefault("tipoAntecedenteTipoDato") as string ?? "texto";
                        var valorRaw = dict.GetValueOrDefault("valor") as string ?? "";
                        var valor = tipoDato switch
                        {
                            "boolean" => valorRaw.Equals("true", StringComparison.OrdinalIgnoreCase) ? "Sí" : "No",
                            _ => valorRaw
                        };

                        model.Antecedentes.Add(new AntecedenteEpicrisis
                        {
                            Nombre = nombre,
                            Valor = valor,
                            Categoria = dict.GetValueOrDefault("tipoAntecedenteCategoria") as string ?? ""
                        });
                    }
                }
            }

            // 11. Obtener datos de la clínica actual (multi-tenant) para encabezado/pie del documento
            await CargarClinicaEnViewModelAsync(model);

            return View(model);
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
