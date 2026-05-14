using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Reportes.Controllers;

[Area("Reportes")]
[Authorize]
public class ReportesController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(ApiClientHelper apiClient, ILogger<ReportesController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Genera un nuevo reporte — para fetch() desde la vista.</summary>
    [HttpPost]
    public async Task<IActionResult> JsonGenerar([FromBody] ReporteFormDto dto)
    {
        _logger.LogInformation("Generar reporte: tipo={Tipo}, fechaInicio={FechaInicio}, fechaFin={FechaFin}",
            dto.Tipo, dto.FechaInicio, dto.FechaFin);

        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Reportes/generar", new
        {
            tipo = dto.Tipo,
            fechaInicio = dto.FechaInicio,
            fechaFin = dto.FechaFin,
            doctorId = dto.DoctorId,
            salaId = dto.SalaId,
            formato = dto.Formato ?? "json"
        });

        if (!success)
        {
            _logger.LogWarning("JsonGenerar API call failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al generar el reporte" });
        }

        var data = ExtractDataObject(response);
        return Ok(new { success = true, data, message = "Reporte generado exitosamente" });
    }

    /// <summary>Obtiene historial de reportes generados.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonHistorial()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Reportes");

        if (!success)
        {
            return Json(new { success = false, data = new List<object>() });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    /// <summary>Exporta un reporte en el formato especificado.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonExportar(Guid id, string formato = "csv")
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Reportes/{id}/exportar?formato={formato}");

        if (!success)
        {
            return BadRequest(new { success = false, message = errorMessage ?? "Error al exportar el reporte" });
        }

        return Ok(new { success = true, data = response });
    }

    /// <summary>Lista doctores disponibles para filtro.</summary>
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

    /// <summary>Lista salas disponibles para filtro.</summary>
    [HttpGet]
    public async Task<IActionResult> JsonSalas()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Salas");

        if (!success)
        {
            return Json(new { success = false, data = new List<object>() });
        }

        var data = ExtractDataArray(response);
        return Json(new { success = true, data });
    }

    // ========== DTO interno ==========

    public class ReporteFormDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public string? DoctorId { get; set; }
        public string? SalaId { get; set; }
        public string? Formato { get; set; }
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
