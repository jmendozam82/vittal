using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Administracion.Controllers;

/// <summary>
/// Controlador proxy para operaciones administrativas globales del Super Admin.
/// Incluye el workspace switcher multi-tenant (cambio de clínica en sesión).
/// </summary>
[Area("Administracion")]
[Authorize]
public class AdminController : Controller
{
    private readonly ApiClientHelper _api;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApiClientHelper api, ILogger<AdminController> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Cambia la clínica activa del Super Admin en sesión (workspace switcher).
    /// Guarda el ID de la clínica seleccionada en Session["ClinicaOverride"].
    /// </summary>
    [HttpGet("Admin/SwitchClinica/{id:guid}")]
    public IActionResult SwitchClinica(Guid id)
    {
        // Verificar que es Super Admin
        var esSuperAdmin = User.FindFirst("app_es_super_admin")?.Value == "true";
        if (!esSuperAdmin)
        {
            _logger.LogWarning("Intento de SwitchClinica por usuario no Super Admin: {User}", User.Identity?.Name);
            return RedirectToAction("AccessDenied", "Home", new { area = "" });
        }

        // Guardar en sesión la clínica seleccionada
        HttpContext.Session.SetString("ClinicaOverride", id.ToString());
        _logger.LogInformation("Super Admin cambió a clínica: {ClinicaId}", id);

        // Redirigir al dashboard
        return RedirectToAction("Index", "Dashboard", new { area = "Dashboard" });
    }

    /// <summary>
    /// Vuelve a la clínica original del Super Admin (limpia el override).
    /// </summary>
    [HttpGet("Admin/VolverClinicaOriginal")]
    public IActionResult VolverClinicaOriginal()
    {
        HttpContext.Session.Remove("ClinicaOverride");
        _logger.LogInformation("Super Admin volvió a su clínica original");
        return RedirectToAction("Index", "Dashboard", new { area = "Dashboard" });
    }
}
