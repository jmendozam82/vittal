using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Alertas.Controllers;

[Area("Alertas")]
[Authorize]
public class AlertasController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<AlertasController> _logger;

    public AlertasController(ApiClientHelper apiClient, ILogger<AlertasController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (successConfig, configResponse, _) = await _apiClient.GetAsync<JsonElement>("api/ConfiguracionAlertas");
        if (successConfig)
        {
            var config = ExtractDataObject(configResponse);
            ViewBag.ConfiguracionAlertas = config;
        }

        var (successAlertas, alertasResponse, _) = await _apiClient.GetAsync<JsonElement>("api/Alertas?resuelta=false");
        if (successAlertas)
        {
            var alertas = ExtractDataArray(alertasResponse);
            ViewBag.AlertasActivas = alertas;
        }

        return View();
    }

    /// <summary>Obtiene la configuración actual de alertas — para fetch().</summary>
    [HttpGet]
    public async Task<IActionResult> JsonConfiguracion()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/ConfiguracionAlertas");

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Error al cargar configuración" });
        }

        var data = ExtractDataObject(response);
        return Json(new { success = true, data });
    }

    /// <summary>Guarda la configuración de alertas.</summary>
    [HttpPut]
    public async Task<IActionResult> JsonGuardarConfiguracion([FromBody] ConfiguracionAlertaDto dto)
    {
        _logger.LogInformation("Guardar configuración alertas: tiempoMaximo={Tiempo}, activo={Activo}",
            dto.TiempoEsperaMaximoMinutos, dto.Activo);

        var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>("api/ConfiguracionAlertas", new
        {
            tiempoEsperaMaximoMinutos = dto.TiempoEsperaMaximoMinutos,
            activo = dto.Activo,
            notificacionSonido = dto.NotificacionSonido,
            intervaloRevisionSegundos = dto.IntervaloRevisionSegundos
        });

        if (!success)
        {
            _logger.LogWarning("JsonGuardarConfiguracion failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al guardar configuración" });
        }

        var data = ExtractDataObject(response);
        return Ok(new { success = true, data, message = "Configuración guardada exitosamente" });
    }

    /// <summary>Obtiene las alertas activas (no resueltas).</summary>
    [HttpGet]
    public async Task<IActionResult> JsonAlertasActivas()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Alertas/no-resueltas");

        if (!success)
        {
            return Json(new { success = false, data = new List<object>() });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>Obtiene todas las alertas opcionalmente filtradas.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonAlertas([FromQuery] bool? resuelta = null)
    {
        var endpoint = resuelta.HasValue
            ? $"api/Alertas?resuelta={resuelta.Value.ToString().ToLower()}"
            : "api/Alertas";

        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(endpoint);

        if (!success)
        {
            return Json(new { success = false, data = new List<object>() });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>Resuelve una alerta manualmente.</summary>
    [HttpPost]
    public async Task<IActionResult> JsonResolverAlerta(Guid alertaId)
    {
        _logger.LogInformation("Resolver alerta: {AlertaId}", alertaId);

        var (success, _, errorMessage) = await _apiClient.PostAsync<JsonElement>($"api/Alertas/{alertaId}/resolver", new { });

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al resolver la alerta" });
        }

        return Ok(new { success = true, message = "Alerta resuelta correctamente" });
    }

    /// <summary>Ejecuta verificación manual de tiempos de espera.</summary>
    [HttpPost]
    public async Task<IActionResult> JsonVerificarAlertas()
    {
        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Alertas/verificar", new { });

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al verificar alertas" });
        }

        var count = 0;
        var dataObj = ExtractDataObject(response);
        if (dataObj?.TryGetValue("data", out var countVal) == true && countVal is double d)
        {
            count = (int)d;
        }

        return Ok(new { success = true, count, message = $"Verificación completada. {count} alerta(s) generada(s)." });
    }

    // ========== DTO interno ==========

    public class ConfiguracionAlertaDto
    {
        public int TiempoEsperaMaximoMinutos { get; set; } = 30;
        public bool Activo { get; set; } = true;
        public bool NotificacionSonido { get; set; } = true;
        public int IntervaloRevisionSegundos { get; set; } = 60;
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
