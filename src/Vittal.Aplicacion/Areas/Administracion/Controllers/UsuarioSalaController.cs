using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Administracion.Controllers;

/// <summary>
/// DTO interno para el formulario de asignación doctor-sala.
/// </summary>
public class AsignarDoctorFormDto
{
    public Guid SalaId { get; set; }
    public Guid UsuarioId { get; set; }
}

[Area("Administracion")]
[Authorize]
public class UsuarioSalaController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<UsuarioSalaController> _logger;

    public UsuarioSalaController(ApiClientHelper apiClient, ILogger<UsuarioSalaController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>Vista principal de asignación de doctores a salas</summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // ===================== JSON PROXY ENDPOINTS =====================

    /// <summary>Lista todas las salas activas de la clínica</summary>
    [HttpGet]
    public async Task<IActionResult> JsonSalas()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Salas");
        if (!success)
        {
            _logger.LogWarning("JsonSalas API call failed: {Error}", errorMessage);
            return Json(new { success = false, message = errorMessage ?? "Error al cargar salas" });
        }
        var data = ExtractDataArray(response);
        return Json(new { success = true, data = data });
    }

    /// <summary>Lista los doctores activos de la clínica (para asignar)</summary>
    [HttpGet]
    public async Task<IActionResult> JsonDoctores()
    {
        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Usuarios/doctores");
        if (!success)
        {
            _logger.LogWarning("JsonDoctores API call failed: {Error}", errorMessage);
            return Json(new { success = false, message = errorMessage ?? "Error al cargar doctores" });
        }
        var data = ExtractDataArray(response);
        return Json(new { success = true, data = data });
    }

    /// <summary>Lista las asignaciones activas de una sala</summary>
    [HttpGet]
    public async Task<IActionResult> JsonAsignaciones(Guid salaId)
    {
        if (salaId == Guid.Empty)
            return Json(new { success = false, message = "Debe especificar una sala." });

        var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/UsuariosSalas/sala/{salaId}");
        if (!success)
        {
            _logger.LogWarning("JsonAsignaciones API call failed: {Error}", errorMessage);
            return Json(new { success = false, message = errorMessage ?? "Error al cargar asignaciones" });
        }
        var data = ExtractDataArray(response);
        return Json(new { success = true, data = data });
    }

    /// <summary>Asigna un doctor a una sala</summary>
    [HttpPost]
    public async Task<IActionResult> JsonAsignar([FromBody] AsignarDoctorFormDto dto)
    {
        if (dto.SalaId == Guid.Empty || dto.UsuarioId == Guid.Empty)
            return BadRequest(new { success = false, message = "Debe especificar sala y doctor." });

        _logger.LogInformation("Asignando doctor {UsuarioId} a sala {SalaId}", dto.UsuarioId, dto.SalaId);

        var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/UsuariosSalas",
            new { usuarioId = dto.UsuarioId, salaId = dto.SalaId });

        if (!success)
        {
            _logger.LogWarning("JsonAsignar API call failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al asignar doctor" });
        }

        return Ok(new { success = true, message = "Doctor asignado correctamente" });
    }

    /// <summary>Desasigna un doctor de una sala (baja lógica)</summary>
    [HttpPatch]
    public async Task<IActionResult> JsonDesasignar(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest(new { success = false, message = "Id de asignación inválido." });

        _logger.LogInformation("Desasignando doctor de sala (asignacionId={Id})", id);

        var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>(
            $"api/UsuariosSalas/{id}/desactivar", null);

        if (!success)
        {
            _logger.LogWarning("JsonDesasignar API call failed: {Error}", errorMessage);
            return BadRequest(new { success = false, message = errorMessage ?? "Error al desasignar doctor" });
        }

        return Ok(new { success = true, message = "Doctor desasignado correctamente" });
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
