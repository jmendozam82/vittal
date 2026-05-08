using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.Aplicacion.Helpers;
using Vittal.Aplicacion.Models;

namespace Vittal.Aplicacion.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApiClientHelper _apiClient;

    public HomeController(ILogger<HomeController> logger, ApiClientHelper apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        var esAdmin = User.FindFirst("app_es_admin") is Claim adminClaim && bool.TryParse(adminClaim.Value, out var isAdmin) && isAdmin;
        var modulosPermitidos = new HashSet<string>();

        if (esAdmin)
        {
            // Admin ve todos los módulos
            ViewBag.EsAdmin = true;
        }
        else
        {
            // Usuario no-admin: consultar permisos desde la API
            var perfilId = User.FindFirst("app_perfil_id") is Claim perfilClaim && Guid.TryParse(perfilClaim.Value, out var perfilIdVal) ? perfilIdVal : Guid.Empty;
            if (perfilId != Guid.Empty)
            {
                var (success, responseJson, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Permisos/perfil/{perfilId}");

                if (success && responseJson.ValueKind == JsonValueKind.Object)
                {
                    // Extraer el array "data" de la respuesta
                    if (responseJson.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var puedeLeer = item.TryGetProperty("puedeLeer", out var leerProp) && leerProp.GetBoolean();
                            if (puedeLeer && item.TryGetProperty("moduloClave", out var claveProp))
                            {
                                var clave = claveProp.GetString();
                                if (!string.IsNullOrEmpty(clave))
                                {
                                    modulosPermitidos.Add(clave);
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No se pudieron obtener permisos para perfil {PerfilId}: {Error}",
                        perfilId, errorMessage);
                }
            }

            ViewBag.EsAdmin = false;
        }

        ViewBag.ModulosPermitidos = modulosPermitidos;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
