using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Expedientes.Controllers
{
    /// <summary>
    /// DTO interno para recibir datos del formulario de constancias desde el cliente.
    /// </summary>
    public class ConstanciaFormDto
    {
        public string ExpedienteId { get; set; } = string.Empty;
        public string? HojaCitaId { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string TipoConstancia { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string? FechaEmision { get; set; }
        public int? DiasReposo { get; set; }
        public string? EspecialistaReferido { get; set; }
    }

    /// <summary>
    /// ViewModel para la vista de impresión de constancia médica.
    /// </summary>
    public class ConstanciaPrintViewModel
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
        public string TipoConstancia { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string FechaEmision { get; set; } = string.Empty;
        public int? DiasReposo { get; set; }
        public string? EspecialistaReferido { get; set; }
    }

    [Area("Expedientes")]
    [Authorize]
    public class ConstanciasController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<ConstanciasController> _logger;

        public ConstanciasController(ApiClientHelper apiClient, ILogger<ConstanciasController> logger)
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
        public async Task<IActionResult> Details(Guid id)
        {
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Constancias/{id}");

            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response);
            if (data == null)
            {
                TempData["Error"] = "Constancia no encontrada.";
                return RedirectToAction("Index");
            }

            ViewBag.Constancia = data;
            return View();
        }

        // ===================== IMPRIMIR CONSTANCIA =====================

        /// <summary>
        /// Muestra la vista de impresión de constancia médica.
        /// Renderiza una página optimizada con @media print para imprimir.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ImprimirConstancia(Guid id)
        {
            // 1. Obtener la constancia
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Constancias/{id}");
            if (!success)
            {
                TempData["Error"] = "No se encontró la constancia.";
                return RedirectToAction("Index");
            }

            var data = ExtractDataObject(response) as Dictionary<string, object?>;
            if (data == null)
            {
                TempData["Error"] = "No se encontró la constancia.";
                return RedirectToAction("Index");
            }

            var model = new ConstanciaPrintViewModel
            {
                DoctorNombre = data.GetValueOrDefault("doctorNombre") as string ?? "",
                PacienteNombre = data.GetValueOrDefault("pacienteNombre") as string ?? "",
                TipoConstancia = data.GetValueOrDefault("tipoConstancia") as string ?? "",
                Contenido = data.GetValueOrDefault("contenido") as string ?? "",
                DiasReposo = data.GetValueOrDefault("diasReposo") is double dr ? (int?)dr : null,
                EspecialistaReferido = data.GetValueOrDefault("especialistaReferido") as string,
                NumeroDocumento = "CON-" + id.ToString("N").Substring(0, 8).ToUpperInvariant()
            };

            // Formatear fecha de emision
            var fechaRaw = data.GetValueOrDefault("fechaEmision") as string;
            if (!string.IsNullOrWhiteSpace(fechaRaw) && DateTime.TryParse(fechaRaw, out var fechaDt))
                model.FechaEmision = fechaDt.ToString("dd/MM/yyyy");
            else
                model.FechaEmision = fechaRaw ?? DateTime.UtcNow.ToString("dd/MM/yyyy");

            // 2. Obtener datos del paciente vía el expediente (módulo 'expedientes' — accesible para el Doctor)
            var expedienteId = data.GetValueOrDefault("expedienteId")?.ToString();
            if (!string.IsNullOrWhiteSpace(expedienteId) && Guid.TryParse(expedienteId, out var constExpId))
            {
                var (pacInfoSuccess, pacInfoResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/{constExpId}/paciente-info");
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

            // 3. Obtener datos de la clínica actual (multi-tenant) para encabezado/pie del documento
            var (cliSuccess, cliResponse, _) = await _apiClient.GetAsync<JsonElement>("api/Clinicas/current-info");
            if (cliSuccess)
            {
                var clinica = ExtractDataObject(cliResponse) as Dictionary<string, object?>;
                if (clinica != null)
                {
                    model.ClinicaNombre = clinica.GetValueOrDefault("nombre") as string ?? model.ClinicaNombre;
                    model.ClinicaDireccion = clinica.GetValueOrDefault("direccion") as string ?? "";
                    model.ClinicaTelefono = clinica.GetValueOrDefault("telefono") as string ?? "";
                    model.ClinicaEmail = clinica.GetValueOrDefault("email") as string ?? "";
                    model.LogoUrl = clinica.GetValueOrDefault("logoUrl") as string;
                }
            }

            return View(model);
        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todas las constancias — para fetch() desde la vista Index</summary>
        [HttpGet]
        public async Task<IActionResult> JsonListar([FromQuery] Guid? expedienteId)
        {
            var url = expedienteId.HasValue
                ? $"api/Constancias?expedienteId={expedienteId.Value}"
                : "api/Constancias";

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)
            {
                _logger.LogWarning("JsonListar API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar constancias" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Lista expedientes/pacientes para el dropdown — para fetch() desde Create</summary>
        [HttpGet]
        public async Task<IActionResult> JsonListarPacientes()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Expedientes");

            if (!success)
            {
                _logger.LogWarning("JsonListarPacientes API call failed: {Error}", errorMessage);
                return Json(new { success = false, message = errorMessage ?? "Error al cargar expedientes" });
            }

            var data = ExtractDataArray(response);
            return Json(new { success = true, data = data });
        }

        /// <summary>Lista doctores para el dropdown — para fetch() desde Create</summary>
        [HttpGet]
        public async Task<IActionResult> JsonListarDoctores()
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Usuarios");

            if (!success)
            {
                _logger.LogWarning("JsonListarDoctores API call failed: {Error}", errorMessage);
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

        /// <summary>Crea una nueva constancia — para fetch() desde la vista Create</summary>
        [HttpPost]
        public async Task<IActionResult> JsonCrear([FromBody] ConstanciaFormDto dto)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(dto.ExpedienteId) || !Guid.TryParse(dto.ExpedienteId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un expediente." });
            }
            if (string.IsNullOrWhiteSpace(dto.DoctorId) || !Guid.TryParse(dto.DoctorId, out _))
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un doctor." });
            }
            if (string.IsNullOrWhiteSpace(dto.TipoConstancia))
            {
                return BadRequest(new { success = false, message = "El tipo de constancia es obligatorio." });
            }
            if (string.IsNullOrWhiteSpace(dto.Contenido))
            {
                return BadRequest(new { success = false, message = "El contenido de la constancia es obligatorio." });
            }

            _logger.LogInformation("JsonCrear constancia: expediente={ExpedienteId}, tipo={TipoConstancia}",
                dto.ExpedienteId, dto.TipoConstancia);

            var payload = new
            {
                expedienteId = dto.ExpedienteId,
                hojaCitaId = string.IsNullOrWhiteSpace(dto.HojaCitaId) ? null : dto.HojaCitaId,
                doctorId = dto.DoctorId,
                tipoConstancia = dto.TipoConstancia,
                contenido = dto.Contenido,
                fechaEmision = string.IsNullOrWhiteSpace(dto.FechaEmision) ? null : dto.FechaEmision,
                diasReposo = dto.DiasReposo,
                especialistaReferido = string.IsNullOrWhiteSpace(dto.EspecialistaReferido) ? null : dto.EspecialistaReferido
            };

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Constancias", payload);

            if (!success)
            {
                _logger.LogWarning("JsonCrear constancia API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear constancia" });
            }

            var data = ExtractDataObject(response);
            return Ok(new { success = true, data = data, message = "Constancia creada exitosamente" });
        }

        /// <summary>Anula/desactiva una constancia — para fetch() desde la vista Index/Details</summary>
        [HttpPatch]
        public async Task<IActionResult> JsonDesactivar(Guid id)
        {
            _logger.LogInformation("JsonDesactivar constancia: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Constancias/{id}/desactivar", null);

            if (!success)
            {
                _logger.LogWarning("JsonDesactivar constancia API call failed: {Error}", errorMessage);
                return BadRequest(new { success = false, message = errorMessage ?? "Error al anular constancia" });
            }

            return Ok(new { success = true, message = "Constancia anulada exitosamente" });
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
