using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Agenda.Controllers;

/// <summary>
/// Controller MVC para el módulo de Agenda (HU21).
/// Gestiona la vista visual de citas en modo Día, 5 Días, 7 Días y Mes.
/// Se comunica con la API REST de Citas mediante ApiClientHelper.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
[Area("Agenda")]
[Authorize]
public class AgendaController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<AgendaController> _logger;

    public AgendaController(ApiClientHelper apiClient, ILogger<AgendaController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────
    //  VISTAS PRINCIPALES
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Vista principal de la Agenda.
    /// Renderiza el shell HTML; los datos se cargan vía JSON.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Vista de creación/edición de cita (modo formulario completo).
    /// </summary>
    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        ViewBag.CitaId = id;
        return View();
    }

    // ──────────────────────────────────────────────────────────────
    //  JSON PROXY ENDPOINTS — para fetch() desde JavaScript
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todas las citas activas de la clínica.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonCitas([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Citas");

        if (!success)
        {
            _logger.LogWarning("JsonCitas API call failed: {Error}", errorMessage);
            return Json(new { success = false, message = errorMessage ?? "Error al cargar citas" });
        }

        var data = ExtractDataArray(response);

        // Filtrar por rango de fechas si se especifica
        if (!string.IsNullOrEmpty(desde) && !string.IsNullOrEmpty(hasta))
        {
            if (DateTime.TryParse(desde, out var desdeDate) && DateTime.TryParse(hasta, out var hastaDate))
            {
                data = data.Where(c =>
                {
                    if (c is Dictionary<string, object?> dict &&
                        dict.TryGetValue("fechaCita", out var fc) &&
                        fc is string fcStr &&
                        DateTime.TryParse(fcStr[..10], out var fcDate))
                    {
                        return fcDate >= desdeDate && fcDate <= hastaDate;
                    }
                    return false;
                }).ToList();
            }
        }

        return Json(new { success = true, data });
    }

    /// <summary>
    /// Obtiene citas para un rango de fechas específico (optimizado para calendario).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonCitasRango([FromQuery] string desde, [FromQuery] string hasta)
    {
        // Obtenemos todas y filtramos del lado servidor (sin endpoint específico en API)
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Citas");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar citas" });
        }

        var data = ExtractDataArray(response);

        if (!string.IsNullOrEmpty(desde) && !string.IsNullOrEmpty(hasta))
        {
            if (DateTime.TryParse(desde, out var desdeDate) && DateTime.TryParse(hasta, out var hastaDate))
            {
                data = data.Where(c =>
                {
                    if (c is Dictionary<string, object?> dict &&
                        dict.TryGetValue("fechaCita", out var fc) &&
                        fc is string fcStr &&
                        DateTime.TryParse(fcStr[..10], out var fcDate))
                    {
                        return fcDate >= desdeDate && fcDate <= hastaDate;
                    }
                    return false;
                }).ToList();
            }
        }

        return Json(new { success = true, data });
    }

    /// <summary>
    /// Obtiene los pacientes para el autocompletado/buscador de la agenda.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonPacientes()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Pacientes");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar pacientes" });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>
    /// Busca pacientes por término (para autocompletado rápido).
    /// </summary>
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
            return Json(new { success = false, message = errorMessage ?? "Error al buscar pacientes" });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>
    /// Obtiene los doctores para los filtros y creación de citas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonDoctores()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Usuarios");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar doctores" });
        }

        var data = ExtractDataArray(response);

        // Filtrar solo doctores
        var doctores = new List<object>();
        foreach (var item in data)
        {
            if (item is Dictionary<string, object?> dict)
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
    /// Obtiene las salas para el dropdown de creación de citas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonSalas()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Salas");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar salas" });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>
    /// Obtiene el horario de atención de la clínica actual del usuario.
    /// Se usa en el frontend para validar horarios al crear/editar citas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonHorarioClinica()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Clinicas/current-schedule");

        if (!success)
        {
            // Si falla, devolver vacío (no bloquear la agenda)
            return Json(new { success = true, data = new { horarioApertura = (string?)null, horarioCierre = (string?)null, diasAtencion = (string?)null } });
        }

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(response!.ToString());
            if (doc.RootElement.TryGetProperty("data", out var dataProp))
            {
                var horarioApertura = dataProp.TryGetProperty("horarioApertura", out var ha) && ha.ValueKind != JsonValueKind.Null ? ha.GetString() : null;
                var horarioCierre = dataProp.TryGetProperty("horarioCierre", out var hc) && hc.ValueKind != JsonValueKind.Null ? hc.GetString() : null;
                var diasAtencion = dataProp.TryGetProperty("diasAtencion", out var da) && da.ValueKind != JsonValueKind.Null ? da.GetString() : null;

                return Json(new
                {
                    success = true,
                    data = new { horarioApertura, horarioCierre, diasAtencion }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al parsear horario de clínica");
        }

        return Json(new { success = true, data = new { horarioApertura = (string?)null, horarioCierre = (string?)null, diasAtencion = (string?)null } });
    }

    // ──────────────────────────────────────────────────────────────
    //  OPERACIONES CRUD (proxy a la API)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva cita desde el modal de la agenda.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> JsonCrear([FromBody] CitaFormDto dto)
    {
        if (dto.PacienteId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar un paciente." });
        }
        if (dto.DoctorId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar un doctor." });
        }

        _logger.LogInformation("JsonCrear cita: paciente={PacienteId}, doctor={DoctorId}, fecha={Fecha}, hora={Hora}",
            dto.PacienteId, dto.DoctorId, dto.FechaCita.ToString("yyyy-MM-dd"), dto.HoraCita);

        var payload = new
        {
            pacienteId = dto.PacienteId,
            doctorId = dto.DoctorId,
            salaId = dto.SalaId,
            fechaCita = dto.FechaCita.ToString("yyyy-MM-dd"),
            horaCita = dto.HoraCita,
            horaFin = dto.HoraFin,
            lugar = dto.Lugar,
            motivo = dto.Motivo,
            estado = "agendada",
            notas = dto.Notas
        };

        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Citas", payload);

        if (!success)
        {
            _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al crear cita" });
        }

        var data = ExtractDataObject(response);

        // Opción D: verificar si el paciente tiene expediente para informar al usuario.
        // No bloquea la creación de la cita — solo devuelve el flag para mostrar un aviso.
        bool sinExpediente = false;
        try
        {
            var (expCheck, _, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/paciente/{dto.PacienteId}");
            // El GET falla (404) cuando el paciente no tiene expediente
            sinExpediente = !expCheck;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo verificar expediente del paciente {PacienteId} al crear cita.", dto.PacienteId);
        }

        return Ok(new { success = true, data, message = "Cita creada exitosamente", sinExpediente });
    }

    /// <summary>
    /// Actualiza una cita existente.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] CitaFormDto dto)
    {
        if (dto.PacienteId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar un paciente." });
        }
        if (dto.DoctorId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar un doctor." });
        }

        _logger.LogInformation("JsonActualizar cita: id={Id}", id);

        var payload = new
        {
            pacienteId = dto.PacienteId,
            doctorId = dto.DoctorId,
            salaId = dto.SalaId,
            fechaCita = dto.FechaCita.ToString("yyyy-MM-dd"),
            horaCita = dto.HoraCita,
            horaFin = dto.HoraFin,
            lugar = dto.Lugar,
            motivo = dto.Motivo,
            estado = dto.Estado,
            notas = dto.Notas
        };

        var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Citas/{id}", payload);

        if (!success)
        {
            _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar cita" });
        }

        var data = ExtractDataObject(response);
        return Ok(new { success = true, data, message = "Cita actualizada exitosamente" });
    }

    /// <summary>
    /// Cambia el estado de una cita (agendada → en_espera → en_atencion → atendida).
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> JsonCambiarEstado(Guid id, [FromQuery] string estado)
    {
        _logger.LogInformation("JsonCambiarEstado: id={Id}, nuevoEstado={Estado}", id, estado);

        // Primero obtenemos la cita actual para tener sus datos
        var (getSuccess, getResponse, getError) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{id}");
        if (!getSuccess)
        {
            return BadRequest(new { success = false, message = "Cita no encontrada." });
        }

        var citaActual = ExtractDataObject(getResponse);
        if (citaActual == null)
        {
            return BadRequest(new { success = false, message = "Error al leer datos de la cita." });
        }

        // Construimos payload con el nuevo estado
        var dict = citaActual as Dictionary<string, object?>;

        var payload = new
        {
            pacienteId = dict?.TryGetValue("pacienteId", out var pId) == true && Guid.TryParse(pId?.ToString(), out var parsedPId) ? parsedPId : Guid.Empty,
            doctorId = dict?.TryGetValue("doctorId", out var dId) == true && Guid.TryParse(dId?.ToString(), out var parsedDId) ? parsedDId : Guid.Empty,
            salaId = dict?.TryGetValue("salaId", out var sId) == true && sId != null && Guid.TryParse(sId.ToString(), out var parsedSId) ? parsedSId : (Guid?)null,
            fechaCita = dict?.TryGetValue("fechaCita", out var fc) == true ? fc?.ToString() : null,
            horaCita = dict?.TryGetValue("horaCita", out var hc) == true ? hc?.ToString() : null,
            horaFin = dict?.TryGetValue("horaFin", out var hf) == true ? hf?.ToString() : null,
            lugar = dict?.TryGetValue("lugar", out var l) == true ? l?.ToString() : null,
            motivo = dict?.TryGetValue("motivo", out var m) == true ? m?.ToString() : null,
            estado = estado,
            notas = dict?.TryGetValue("notas", out var n) == true ? n?.ToString() : null
        };

        var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Citas/{id}", payload);

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al cambiar estado" });
        }

        var data = ExtractDataObject(response);
        return Ok(new { success = true, data, message = $"Cita marcada como '{estado}'" });
    }

    /// <summary>
    /// Desactiva una cita (activo = false). Nunca elimina.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> JsonDesactivar(Guid id)
    {
        _logger.LogInformation("JsonDesactivar cita: id={Id}", id);

        var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Citas/{id}/desactivar", null);

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar cita" });
        }

        return Ok(new { success = true, message = "Cita desactivada exitosamente" });
    }

    // ──────────────────────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────────────────────

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

    private static List<object> EnumerateJsonArray(JsonElement array)
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

    private static T GetValue<T>(Dictionary<string, object?>? dict, string key)
    {
        if (dict == null) return default!;
        if (dict.TryGetValue(key, out var val) && val is T tVal)
        {
            return tVal;
        }
        return default!;
    }
}

/// <summary>
/// DTO interno para recibir datos del formulario de citas desde el cliente.
/// </summary>
public class CitaFormDto
{
    public Guid PacienteId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? SalaId { get; set; }
    public DateTime FechaCita { get; set; }
    public string HoraCita { get; set; } = "08:00";
    public string? HoraFin { get; set; }
    public string? Lugar { get; set; }
    public string? Motivo { get; set; }
    public string Estado { get; set; } = "agendada";
    public string? Notas { get; set; }
}
