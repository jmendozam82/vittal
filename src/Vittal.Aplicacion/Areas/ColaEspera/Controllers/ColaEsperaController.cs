using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    //  HELPERS DE IDENTIDAD (perfil doctor)
    // ──────────────────────────────────────────────────────────────

    /// <summary>Indica si el usuario autenticado tiene perfil de doctor.</summary>
    private bool EsDoctor() => User.FindFirstValue("app_es_doctor") == "true";

    /// <summary>Indica si el usuario autenticado es administrador de clínica.</summary>
    private bool EsAdmin() => User.FindFirstValue("app_es_admin") == "true";

    /// <summary>Indica si el usuario autenticado es super administrador (global).</summary>
    private bool EsSuperAdmin() => User.FindFirstValue("app_es_super_admin") == "true";

    /// <summary>
    /// El avance clínico de una cita (Atender/Completar) SOLO puede realizarlo
    /// el personal asistencial: médico, admin o superadmin. La recepcionista queda
    /// restringida al registro de llegada y cancelación (Opción A — flujo real de clínica).
    /// </summary>
    private bool PuedeGestionarAtencion() => EsDoctor() || EsAdmin() || EsSuperAdmin();

    /// <summary>Obtiene el usuario interno (NameIdentifier) del usuario autenticado.</summary>
    private Guid UsuarioId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Regla de negocio (§12, regla 6): un doctor SOLO opera sobre sus propias citas.
    /// Admin/Gerente/Recepcionista pueden operar sobre todas (depende del permiso del módulo).
    /// </summary>
    private bool PuedeOperarCita(Dictionary<string, object?>? dict)
    {
        if (!EsDoctor()) return true;
        var uid = UsuarioId();
        return dict != null
            && dict.TryGetValue("doctorId", out var dId)
            && dId != null
            && Guid.TryParse(dId.ToString(), out var dGuid)
            && dGuid == uid;
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

        // Filtrar por doctor si se especifica.
        // Regla 6: si el usuario autenticado es doctor, se fuerza SOLO su propia cola
        // (se ignora cualquier doctorId enviado por el cliente para evitar ver/operar la cola de otros).
        Guid? doctorFiltro = null;
        if (EsDoctor())
        {
            var uid = UsuarioId();
            if (uid != Guid.Empty) doctorFiltro = uid;
        }
        else if (!string.IsNullOrEmpty(doctorId) && Guid.TryParse(doctorId, out var docGuid))
        {
            doctorFiltro = docGuid;
        }

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

            // Filtrar por doctor (propio si es doctor, o el seleccionado en el filtro)
            if (doctorFiltro.HasValue)
            {
                var docIdOk = dict.TryGetValue("doctorId", out var dId) && dId is string dIdStr
                    && Guid.TryParse(dIdStr, out var dIdGuid)
                    && dIdGuid == doctorFiltro.Value;

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
    /// Usa api/agenda/catalogos (permiso agenda) en lugar de api/Usuarios (permiso usuarios),
    /// para que el Doctor pueda usar el filtro sin acceso al módulo de usuarios (fix Hallazgo 4b).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> JsonDoctores()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/agenda/catalogos");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar doctores" });
        }

        var data = ExtractCatalogosDataArray(response, "doctores");

        // Filtrar solo doctores (defensa en profundidad: el endpoint ya filtra)
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

        // Regla 6: el doctor solo puede registrar llegada de sus propias citas
        if (!PuedeOperarCita(dict))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, message = "No puede registrar llegada de citas de otro doctor." });
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

        // ── Automatizar línea de tiempo: iniciar paso "Llegada" (paciente en espera) ──
        // El paso queda "en_sala" (en curso) y registra la hora de llegada real.
        await IniciarPasoTimelineAsync(id, "Llegada");

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

        // Opción A: el avance clínico SOLO lo ejecuta el personal asistencial
        // (médico/admin/superadmin); la recepcionista NO puede iniciar atención.
        if (!PuedeGestionarAtencion())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, message = "Solo el personal médico puede iniciar la atención. La recepción solo registra la llegada." });
        }

        // Regla 6: el doctor solo puede atender sus propias citas
        if (!PuedeOperarCita(dict))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, message = "No puede atender citas de otro doctor." });
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

        // ── Automatizar línea de tiempo: finalizar "Llegada" e iniciar "Consulta" ──
        // La espera queda capturada (hora salida de Llegada) y la consulta entra "en_sala".
        await FinalizarPasoTimelineAsync(id, "Llegada");
        await IniciarPasoTimelineAsync(id, "Consulta");

        // Buscar el expediente del paciente para redirigir a la hoja de cita.
        // Si el paciente NO tiene expediente, se crea automáticamente (Opción D):
        // se crea con los datos mínimos de la cita (pacienteId + doctorId) para que
        // la hoja de cita pueda generarse y la cita NUNCA quede huérfana.
        Guid? expedienteId = null;
        var pacienteId = GetNullableGuidValue(dict, "pacienteId");
        if (pacienteId.HasValue)
        {
            var (expSuccess, expResponse, _) = await _apiClient.GetAsync<JsonElement>($"api/Expedientes/paciente/{pacienteId.Value}");
            if (expSuccess)
            {
                var expData = ExtractDataObject(expResponse);
                if (expData is Dictionary<string, object?> expDict)
                {
                    var expId = GetNullableGuidValue(expDict, "id");
                    if (expId.HasValue)
                        expedienteId = expId.Value;
                }
            }

            // No existe expediente → crearlo automáticamente con los datos de la cita
            if (!expedienteId.HasValue)
            {
                _logger.LogInformation("Paciente {PacienteId} sin expediente. Creando expediente automático al atender.",
                    pacienteId.Value);

                var doctorId = GetGuidValue(dict, "doctorId");

                if (doctorId != Guid.Empty)
                {
                    var expPayload = new
                    {
                        pacienteId = pacienteId.Value,
                        doctorId = doctorId,
                        notasGenerales = (string?)null
                    };

                    var (expCreateSuccess, expCreateResponse, expCreateError) =
                        await _apiClient.PostAsync<JsonElement>("api/Expedientes", expPayload);

                    if (expCreateSuccess)
                    {
                        var expCreado = ExtractDataObject(expCreateResponse);
                        if (expCreado is Dictionary<string, object?> expCreadoDict)
                        {
                            var newExpId = GetNullableGuidValue(expCreadoDict, "id");
                            if (newExpId.HasValue)
                            {
                                expedienteId = newExpId.Value;
                                _logger.LogInformation("Expediente automático {ExpedienteId} creado para paciente {PacienteId}.",
                                    newExpId.Value, pacienteId.Value);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No se pudo crear expediente automático para paciente {PacienteId}: {Error}",
                            pacienteId.Value, expCreateError);
                    }
                }
                else
                {
                    _logger.LogWarning("No se pudo crear expediente automático para paciente {PacienteId}: cita sin doctorId.",
                        pacienteId.Value);
                }
            }
        }

        return Ok(new { success = true, message = "Atención iniciada", expedienteId, citaId = id.ToString() });
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

        // Opción A: completar la consulta SOLO lo ejecuta el personal asistencial
        // (médico/admin/superadmin); la recepcionista NO puede finalizar atención.
        if (!PuedeGestionarAtencion())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, message = "Solo el personal médico puede completar la consulta." });
        }

        // Regla 6: el doctor solo puede completar sus propias citas
        if (!PuedeOperarCita(dict))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, message = "No puede completar citas de otro doctor." });
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

        // ── Automatizar línea de tiempo: finalizar "Consulta" y completar "Salida" ──
        // La consulta queda capturada y el paso Salida se marca completado (salida real).
        await FinalizarPasoTimelineAsync(id, "Consulta");
        await IniciarPasoTimelineAsync(id, "Salida");
        await FinalizarPasoTimelineAsync(id, "Salida");

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

        // Regla 6: el doctor solo puede cancelar sus propias citas
        if (!PuedeOperarCita(dict))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, message = "No puede cancelar citas de otro doctor." });
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

    // ── Automatización de Línea de Tiempo (HU19) ──────────────────
    // La Cola de Espera dispara la línea de tiempo automáticamente:
    //   Llegó      → inicia paso "Llegada"
    //   Atender    → finaliza "Llegada" e inicia "Consulta"
    //   Completar  → finaliza "Consulta" y completa "Salida"
    // Así, el médico NO necesita acciones manuales en la línea de tiempo.

    /// <summary>
    /// Obtiene el ID del paso de línea de tiempo por su nombre para una cita.
    /// </summary>
    private async Task<Guid?> GetPasoIdByNombreAsync(Guid citaId, string nombrePaso)
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/LineaTiempo/cita/{citaId}");

        if (!success)
        {
            _logger.LogWarning("Timeline lookup failed for cita {CitaId} paso {Paso}: {Error}", citaId, nombrePaso, errorMessage);
            return null;
        }

        var data = ExtractDataArray(response);
        foreach (var item in data)
        {
            if (item is Dictionary<string, object?> dict)
            {
                var nombre = GetValue<string>(dict, "nombrePaso");
                var id = GetValue<string>(dict, "id");
                if (string.Equals(nombre, nombrePaso, StringComparison.OrdinalIgnoreCase)
                    && Guid.TryParse(id, out var pasoId))
                {
                    return pasoId;
                }
            }
        }

        _logger.LogWarning("Paso {Paso} no encontrado en timeline de cita {CitaId}", nombrePaso, citaId);
        return null;
    }

    /// <summary>
    /// Inicia un paso de la línea de tiempo por nombre (estado "en_sala").
    /// No bloquea la transición de la cola si falla (solo se loguea).
    /// </summary>
    private async Task IniciarPasoTimelineAsync(Guid citaId, string nombrePaso)
    {
        try
        {
            var pasoId = await GetPasoIdByNombreAsync(citaId, nombrePaso);
            if (!pasoId.HasValue) return;

            await _apiClient.PostAsync<JsonElement>($"api/LineaTiempo/{pasoId.Value}/iniciar", new { });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo iniciar paso {Paso} de cita {CitaId}", nombrePaso, citaId);
        }
    }

    /// <summary>
    /// Finaliza un paso de la línea de tiempo por nombre (estado "completado").
    /// No bloquea la transición de la cola si falla (solo se loguea).
    /// </summary>
    private async Task FinalizarPasoTimelineAsync(Guid citaId, string nombrePaso)
    {
        try
        {
            var pasoId = await GetPasoIdByNombreAsync(citaId, nombrePaso);
            if (!pasoId.HasValue) return;

            await _apiClient.PostAsync<JsonElement>($"api/LineaTiempo/{pasoId.Value}/finalizar", new { });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo finalizar paso {Paso} de cita {CitaId}", nombrePaso, citaId);
        }
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
                return EnumerateJsonArray(arr);
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
