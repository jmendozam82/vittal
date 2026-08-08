using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Dashboard.Controllers;

[Area("Dashboard")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApiClientHelper apiClient, ILogger<DashboardController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (success, response, _) = await _apiClient.GetAsync<JsonElement>("api/Dashboard/data");

        if (success)
        {
            var data = ExtractDataObject(response);
            ViewBag.DashboardData = data;
        }
        else
        {
            _logger.LogWarning("Dashboard/Index - API call failed");
            TempData["Warning"] = "No se pudieron cargar algunos datos del dashboard.";
        }

        var userName = User.Identity?.Name ?? "Usuario";
        ViewBag.NombreUsuario = userName;
        ViewBag.FechaHoy = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-MX"));

        return View();
    }

    /// <summary>Obtiene datos frescos del dashboard para polling via JavaScript.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonDashboardData([FromQuery] string? fecha)
    {
        var endpoint = string.IsNullOrEmpty(fecha)
            ? "api/Dashboard/data"
            : $"api/Dashboard/data?fecha={fecha}";

        var (success, response, _) = await _apiClient.GetAsync<JsonElement>(endpoint);

        if (!success)
        {
            return Json(new { success = false, message = "Error al cargar datos del dashboard" });
        }

        var data = ExtractDataObject(response);
        return Json(new { success = true, data });
    }

    /// <summary>Obtiene conteo de notificaciones no leídas para el badge del navbar.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonNotificacionesNoLeidas()
    {
        var (success, response, _) = await _apiClient.GetAsync<JsonElement>("api/Notificaciones/no-leidas-count");

        // El API devuelve ApiResponse<int>: el "data" es un número, no un objeto.
        // Se lee directo del nodo raíz para evitar el bug de extracción de objetos.
        var count = 0;
        if (success && response.ValueKind == JsonValueKind.Object
            && response.TryGetProperty("data", out var dataProp)
            && dataProp.ValueKind == JsonValueKind.Number)
        {
            count = (int)dataProp.GetDouble();
        }

        return Json(new { success = true, count });
    }

    /// <summary>Obtiene últimas notificaciones para el dropdown del navbar.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonUltimasNotificaciones()
    {
        var (success, response, _) = await _apiClient.GetAsync<JsonElement>("api/Notificaciones?leida=false&limit=5");

        if (!success)
        {
            return Json(new { success = false, data = new List<object>() });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>Marca una notificación como leída (proxy al API).</summary>
    [HttpPost]
    public async Task<IActionResult> JsonMarcarLeida([FromQuery] Guid id)
    {
        var (success, _, _) = await _apiClient.PutAsync<JsonElement>($"api/Notificaciones/{id}/leer", new { });
        return Json(new { success });
    }

    /// <summary>Marca todas las notificaciones de la clínica como leídas (proxy al API).</summary>
    [HttpPost]
    public async Task<IActionResult> JsonMarcarTodasLeidas()
    {
        var (success, _, _) = await _apiClient.PutAsync<JsonElement>("api/Notificaciones/leer-todas", new { });
        return Json(new { success });
    }

    // ========== Helpers ==========

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
