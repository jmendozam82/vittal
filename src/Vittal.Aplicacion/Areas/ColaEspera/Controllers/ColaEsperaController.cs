using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.ColaEspera.Controllers;

/// <summary>
/// Controller MVC para el módulo de Cola de Espera (HU18).
/// Gestiona la cola de pacientes en tiempo real con estado: agendada → en_espera → en_atencion → atendida.
/// Se comunica con la API REST de Citas mediante ApiClientHelper.
/// Historia de Usuario: HU18 — Cola de Espera
/// </summary>
[Area("ColaEspera")]
[Authorize]
public class ColaEsperaController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<ColaEsperaController> _logger;

    public ColaEsperaController(ApiClientHelper apiClient, ILogger<ColaEsperaController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────
    //  VISTA PRINCIPAL
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Vista principal de la Cola de Espera.
    /// Renderiza el shell HTML; los datos se cargan vía JSON.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // ──────────────────────────────────────────────────────────────
    //  JSON PROXY ENDPOINTS — para fetch() desde JavaScript
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene la cola del día actual con estados relevantes (agendada, en_espera, en_atencion).
    /// Filtra por doctor si se especifica.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonCola([FromQuery] string? doctorId)
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Citas");

        if (!success)
        {
            _logger.LogWarning("JsonCola API call failed: {Error}", errorMessage);
            return Json(new { success = false, message = errorMessage ?? "Error al cargar la cola de espera" });
        }

        var data = ExtractDataArray(response);

        // Estados que forman parte de la cola de espera activa
        var estadosCola = new HashSet<string> { "agendada", "en_espera", "en_atencion" };

        // Fecha de hoy (solo comparar YYYY-MM-DD)
        var hoy = DateTime.Today;

        var cola = new List<object>();
        var atendidasHoy = 0;

        foreach (var item in data)
        {
            if (item is not Dictionary<string, object?> dict) continue;

            // Filtrar por fecha de hoy
            var fechaOk = dict.TryGetValue("fechaCita", out var fc) && fc is string fcStr
                && DateTime.TryParse(fcStr[..10], out var fcDate)
                && fcDate.Date == hoy;

            if (!fechaOk) continue;

            // Obtener estado
            var estadoStr = dict.TryGetValue("estado", out var est) && est is string es ? es : null;

            // Contar atendidas de hoy para estadísticas (tarjeta "Hoy Atendidas")
            if (estadoStr == "atendida")
            {
                atendidasHoy++;
                continue; // No agregar a la cola activa
            }

            // Filtrar por estado (solo estados de cola activa)
            if (estadoStr == null || !estadosCola.Contains(estadoStr)) continue;

            // Filtrar por doctor si se especifica
            if (!string.IsNullOrEmpty(doctorId) && Guid.TryParse(doctorId, out var docGuid))
            {
                var docIdOk = dict.TryGetValue("doctorId", out var dId) && dId is string dIdStr
                    && Guid.TryParse(dIdStr, out var dIdGuid)
                    && dIdGuid == docGuid;

                if (!docIdOk) continue;
            }

            cola.Add(dict);
        }

        // Ordenar por hora_cita ASC
        cola = cola.OrderBy(c =>
        {
            if (c is Dictionary<string, object?> d &&
                d.TryGetValue("horaCita", out var hc) && hc is string hcStr &&
                TimeSpan.TryParse(hcStr, out var ts))
                return ts;
            return TimeSpan.MaxValue;
        }).ToList();

        return Json(new { success = true, data = cola, total = cola.Count, stats = new { atendidasHoy } });
    }

    /// <summary>
    /// Obtiene los doctores para el filtro de cola de espera.
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

    // ──────────────────────────────────────────────────────────────
    //  TRANSICIONES DE ESTADO EN LA COLA
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Marca un paciente como "Llegó" (agendada → en_espera).
    /// Registra la hora de llegada automáticamente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> JsonLlegada([FromQuery] Guid id)
    {
        _logger.LogInformation("JsonLlegada: cita={Id}", id);

        // Obtener cita actual
        var (getSuccess, getResponse, getError) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{id}");
        if (!getSuccess)
        {
            return BadRequest(new { success = false, message = "Cita no encontrada." });
        }

        var citaActual = ExtractDataObject(getResponse);
        if (citaActual is not Dictionary<string, object?> dict)
        {
            return BadRequest(new { success = false, message = "Error al leer datos de la cita." });
        }

        // Hora de llegada actual
        var horaActual = DateTime.Now.ToString("HH:mm");

        var payload = new
        {
            pacienteId = GetGuidValue(dict, "pacienteId"),
            doctorId = GetGuidValue(dict, "doctorId"),
            salaId = GetNullableGuidValue(dict, "salaId"),
            fechaCita = dict.TryGetValue("fechaCita", out var fc) && fc is string fcStr ? fcStr[..10] : null,
            horaCita = dict.TryGetValue("horaCita", out var hc) && hc is string hcStr ? hcStr : null,
            horaFin = dict.TryGetValue("horaFin", out var hf) && hf is string hfStr ? hfStr : null,
            horaLlegada = horaActual,
            lugar = dict.TryGetValue("lugar", out var l) && l is string lStr ? lStr : null,
            motivo = dict.TryGetValue("motivo", out var m) && m is string mStr ? mStr : null,
            estado = "en_espera",
            notas = dict.TryGetValue("notas", out var n) && n is string nStr ? nStr : null
        };

        var (success, _, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Citas/{id}", payload);

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al registrar llegada" });
        }

        return Ok(new { success = true, message = "Llegada registrada — paciente en espera" });
    }

    /// <summary>
    /// Inicia la atención de un paciente (en_espera → en_atencion).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> JsonAtender([FromQuery] Guid id)
    {
        _logger.LogInformation("JsonAtender: cita={Id}", id);

        var (getSuccess, getResponse, getError) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{id}");
        if (!getSuccess)
        {
            return BadRequest(new { success = false, message = "Cita no encontrada." });
        }

        var citaActual = ExtractDataObject(getResponse);
        if (citaActual is not Dictionary<string, object?> dict)
        {
            return BadRequest(new { success = false, message = "Error al leer datos de la cita." });
        }

        var payload = new
        {
            pacienteId = GetGuidValue(dict, "pacienteId"),
            doctorId = GetGuidValue(dict, "doctorId"),
            salaId = GetNullableGuidValue(dict, "salaId"),
            fechaCita = dict.TryGetValue("fechaCita", out var fc) && fc is string fcStr ? fcStr[..10] : null,
            horaCita = dict.TryGetValue("horaCita", out var hc) && hc is string hcStr ? hcStr : null,
            horaFin = dict.TryGetValue("horaFin", out var hf) && hf is string hfStr ? hfStr : null,
            horaLlegada = dict.TryGetValue("horaLlegada", out var hl) && hl is string hlStr ? hlStr : null,
            lugar = dict.TryGetValue("lugar", out var l) && l is string lStr ? lStr : null,
            motivo = dict.TryGetValue("motivo", out var m) && m is string mStr ? mStr : null,
            estado = "en_atencion",
            notas = dict.TryGetValue("notas", out var n) && n is string nStr ? nStr : null
        };

        var (success, _, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Citas/{id}", payload);

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al iniciar atención" });
        }

        return Ok(new { success = true, message = "Atención iniciada" });
    }

    /// <summary>
    /// Completa la atención de un paciente (en_atencion → atendida).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> JsonCompletar([FromQuery] Guid id)
    {
        _logger.LogInformation("JsonCompletar: cita={Id}", id);

        var (getSuccess, getResponse, getError) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{id}");
        if (!getSuccess)
        {
            return BadRequest(new { success = false, message = "Cita no encontrada." });
        }

        var citaActual = ExtractDataObject(getResponse);
        if (citaActual is not Dictionary<string, object?> dict)
        {
            return BadRequest(new { success = false, message = "Error al leer datos de la cita." });
        }

        var payload = new
        {
            pacienteId = GetGuidValue(dict, "pacienteId"),
            doctorId = GetGuidValue(dict, "doctorId"),
            salaId = GetNullableGuidValue(dict, "salaId"),
            fechaCita = dict.TryGetValue("fechaCita", out var fc) && fc is string fcStr ? fcStr[..10] : null,
            horaCita = dict.TryGetValue("horaCita", out var hc) && hc is string hcStr ? hcStr : null,
            horaFin = dict.TryGetValue("horaFin", out var hf) && hf is string hfStr ? hfStr : null,
            horaLlegada = dict.TryGetValue("horaLlegada", out var hl) && hl is string hlStr ? hlStr : null,
            lugar = dict.TryGetValue("lugar", out var l) && l is string lStr ? lStr : null,
            motivo = dict.TryGetValue("motivo", out var m) && m is string mStr ? mStr : null,
            estado = "atendida",
            notas = dict.TryGetValue("notas", out var n) && n is string nStr ? nStr : null
        };

        var (success, _, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Citas/{id}", payload);

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al completar atención" });
        }

        return Ok(new { success = true, message = "Atención completada" });
    }

    /// <summary>
    /// Cancela una cita desde la cola de espera.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> JsonCancelar([FromQuery] Guid id)
    {
        _logger.LogInformation("JsonCancelar: cita={Id}", id);

        var (getSuccess, getResponse, getError) = await _apiClient.GetAsync<JsonElement>($"api/Citas/{id}");
        if (!getSuccess)
        {
            return BadRequest(new { success = false, message = "Cita no encontrada." });
        }

        var citaActual = ExtractDataObject(getResponse);
        if (citaActual is not Dictionary<string, object?> dict)
        {
            return BadRequest(new { success = false, message = "Error al leer datos de la cita." });
        }

        var payload = new
        {
            pacienteId = GetGuidValue(dict, "pacienteId"),
            doctorId = GetGuidValue(dict, "doctorId"),
            salaId = GetNullableGuidValue(dict, "salaId"),
            fechaCita = dict.TryGetValue("fechaCita", out var fc) && fc is string fcStr ? fcStr[..10] : null,
            horaCita = dict.TryGetValue("horaCita", out var hc) && hc is string hcStr ? hcStr : null,
            horaFin = dict.TryGetValue("horaFin", out var hf) && hf is string hfStr ? hfStr : null,
            horaLlegada = dict.TryGetValue("horaLlegada", out var hl) && hl is string hlStr ? hlStr : null,
            lugar = dict.TryGetValue("lugar", out var l) && l is string lStr ? lStr : null,
            motivo = dict.TryGetValue("motivo", out var m) && m is string mStr ? mStr : null,
            estado = "cancelada",
            notas = dict.TryGetValue("notas", out var n) && n is string nStr ? nStr : null
        };

        var (success, _, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Citas/{id}", payload);

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al cancelar cita" });
        }

        return Ok(new { success = true, message = "Cita cancelada" });
    }

    // ── Helper para extraer GUIDs desde diccionarios JSON ─────────
    // Los valores JSON se deserializan como strings, NO como Guid.
    // GetValue<Guid> falla (return Guid.Empty). Este helper parsea correctamente.
    private static Guid GetGuidValue(Dictionary<string, object?>? dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var val) && val is string strVal && Guid.TryParse(strVal, out var guid))
            return guid;
        return Guid.Empty;
    }

    private static Guid? GetNullableGuidValue(Dictionary<string, object?>? dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var val) && val is string strVal && Guid.TryParse(strVal, out var guid))
            return guid;
        return null;
    }

    // ──────────────────────────────────────────────────────────────
    //  HELPERS (idénticos a los de AgendaController)
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
