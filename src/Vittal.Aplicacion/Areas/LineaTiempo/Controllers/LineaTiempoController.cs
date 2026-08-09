using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.LineaTiempo.Controllers;

[Area("LineaTiempo")]
[Authorize]
public class LineaTiempoController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<LineaTiempoController> _logger;

    public LineaTiempoController(ApiClientHelper apiClient, ILogger<LineaTiempoController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(Guid? citaId, string? paciente)
    {
        ViewData["FechaHoy"] = DateTime.Now.ToString("yyyy-MM-dd");
        ViewData["CitaId"] = citaId;
        ViewData["PacienteNombre"] = paciente;
        return View();
    }

    /// <summary>Obtiene la línea de tiempo del día — para fetch() desde JavaScript.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonTimelineDelDia([FromQuery] string? fecha, [FromQuery] string? doctorId)
    {
        // Regla 6: un doctor solo ve su propia línea de tiempo (defensa en profundidad).
        if (EsDoctor()) doctorId = UsuarioId().ToString();

        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(fecha)) queryParams.Add($"fecha={fecha}");
        if (!string.IsNullOrEmpty(doctorId)) queryParams.Add($"doctorId={doctorId}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var endpoint = $"api/LineaTiempo/dia{queryString}";

        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(endpoint);

        if (!success)
        {
            _logger.LogWarning("JsonTimelineDelDia API call failed: {Error}", errorMessage);
            return Json(new { success = false, message = errorMessage ?? "Error al cargar la línea de tiempo" });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>Obtiene la línea de tiempo de una cita específica.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonTimelineByCita(Guid citaId)
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/LineaTiempo/cita/{citaId}");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar datos de la cita" });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>Inicia un paso de la línea de tiempo.</summary>
    [HttpPost]
    public async Task<IActionResult> JsonIniciarPaso(Guid pasoId)
    {
        _logger.LogInformation("Iniciar paso: {PasoId}", pasoId);

        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>($"api/LineaTiempo/{pasoId}/iniciar", new { });

        if (!success)
        {
            _logger.LogWarning("JsonIniciarPaso failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al iniciar el paso" });
        }

        var data = ExtractDataObject(response);
        return Ok(new { success = true, data, message = "Paso iniciado correctamente" });
    }

    /// <summary>Finaliza un paso de la línea de tiempo.</summary>
    [HttpPost]
    public async Task<IActionResult> JsonFinalizarPaso(Guid pasoId)
    {
        _logger.LogInformation("Finalizar paso: {PasoId}", pasoId);

        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>($"api/LineaTiempo/{pasoId}/finalizar", new { });

        if (!success)
        {
            _logger.LogWarning("JsonFinalizarPaso failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al finalizar el paso" });
        }

        var data = ExtractDataObject(response);
        return Ok(new { success = true, data, message = "Paso finalizado correctamente" });
    }

    /// <summary>Salta un paso de la línea de tiempo.</summary>
    [HttpPost]
    public async Task<IActionResult> JsonSaltarPaso(Guid pasoId)
    {
        _logger.LogInformation("Saltar paso: {PasoId}", pasoId);

        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>($"api/LineaTiempo/{pasoId}/saltar", new { });

        if (!success)
        {
            _logger.LogWarning("JsonSaltarPaso failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al saltar el paso" });
        }

        return Ok(new { success = true, message = "Paso saltado correctamente" });
    }

    /// <summary>Obtiene lista de doctores para el filtro.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonDoctores()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Usuarios/doctores");

        if (!success)
        {
            return Json(new { success = false, data = new List<object>() });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    // ========== Helpers ==========

    /// <summary>Indica si el usuario autenticado tiene perfil de doctor.</summary>
    private bool EsDoctor() => User.FindFirstValue("app_es_doctor") == "true";

    /// <summary>Obtiene el usuario interno (NameIdentifier) del usuario autenticado.</summary>
    private Guid UsuarioId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : Guid.Empty;
    }

    private static Dictionary<string, object?>? ExtractDataObject(JsonElement? response)
    {
        if (response == null) return null;

        try
        {
            var r = response.Value;
            if (r.TryGetProperty("data", out var dataProp))
                return JsonElementToDictionary(dataProp);
            return JsonElementToDictionary(r);
        }
        catch { return null; }
    }

    private static IEnumerable<object> ExtractDataArray(JsonElement? response)
    {
        if (response == null) return new List<object>();

        try
        {
            var r = response.Value;
            if (r.TryGetProperty("data", out var dataProp))
                return EnumerateJsonArray(dataProp);
            if (r.ValueKind == JsonValueKind.Array)
                return EnumerateJsonArray(r);
        }
        catch { }

        return new List<object>();
    }

    private static IEnumerable<object> EnumerateJsonArray(JsonElement array)
    {
        var list = new List<object>();
        foreach (var item in array.EnumerateArray())
            list.Add(JsonElementToDictionary(item));
        return list;
    }

    private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
                dict[prop.Name] = JsonElementToValue(prop.Value);
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
