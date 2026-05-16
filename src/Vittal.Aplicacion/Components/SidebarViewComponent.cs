using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Components;

/// <summary>
/// ViewComponent que renderiza el menú lateral (sidebar) con sub-módulos colapsables,
/// filtrando items según los permisos READ del perfil del usuario autenticado.
/// </summary>
public class SidebarViewComponent : ViewComponent
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<SidebarViewComponent> _logger;

    // Mapa de secciones del sidebar a sus claves de módulo en BD
    private static readonly string[] ModulosDashboard = { "dashboard" };
    private static readonly string[] ModulosLineaTiempo = { "linea_tiempo" };
    private static readonly string[] ModulosColaEspera = { "cola_espera" };
    private static readonly string[] ModulosAdministracion = { "perfiles", "usuarios", "permisos", "salas" };
    private static readonly string[] ModulosCatalogos =
    {
        "pacientes", "medicamentos", "clinicas", "tipos_cirugia", "cirugias",
        "tipos_dx", "diagnosticos", "tratamientos", "recomendaciones", "examenes",
        "tipos_antecedente", "tipos_signo_vital"
    };
    private static readonly string[] ModulosAgenda = { "agenda" };
    private static readonly string[] ModulosExpedientes = { "expedientes" };
    private static readonly string[] ModulosReportes = { "reportes" };
    private static readonly string[] ModulosAlertas = { "alertas" };

    public SidebarViewComponent(ApiClientHelper apiClient, ILogger<SidebarViewComponent> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new SidebarViewModel
        {
            CurrentArea = ViewContext.RouteData.Values["area"]?.ToString() ?? "",
            CurrentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? ""
        };

        var claimsUser = User as System.Security.Claims.ClaimsPrincipal;

        // Admin/SuperAdmin ven todo
        var esAdmin = claimsUser?.FindFirst("app_es_admin") is System.Security.Claims.Claim adminClaim
            && bool.TryParse(adminClaim.Value, out var isAdmin) && isAdmin;

        var esSuperAdmin = claimsUser?.FindFirst("app_es_super_admin") is System.Security.Claims.Claim superAdminClaim
            && bool.TryParse(superAdminClaim.Value, out var isSuperAdmin) && isSuperAdmin;

        if (esAdmin || esSuperAdmin)
        {
            MostrarTodo(model);
            return View(model);
        }

        // Usuario normal: consultar permisos desde la API
        var perfilId = claimsUser?.FindFirst("app_perfil_id") is System.Security.Claims.Claim perfilClaim
            && Guid.TryParse(perfilClaim.Value, out var perfilIdVal) ? perfilIdVal : Guid.Empty;

        if (perfilId == Guid.Empty)
        {
            _logger.LogWarning("No se encontró perfil ID en claims para el sidebar");
            return View(model);
        }

        try
        {
            var (success, responseJson, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/Permisos/perfil/{perfilId}");

            if (success && responseJson.ValueKind == JsonValueKind.Object)
            {
                var modulosPermitidos = new HashSet<string>();

                if (responseJson.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var puedeLeer = item.TryGetProperty("puedeLeer", out var leerProp) && leerProp.GetBoolean();
                        if (puedeLeer && item.TryGetProperty("moduloClave", out var claveProp))
                        {
                            var clave = claveProp.GetString();
                            if (!string.IsNullOrEmpty(clave))
                                modulosPermitidos.Add(clave);
                        }
                    }
                }

                AplicarPermisos(model, modulosPermitidos);
            }
            else
            {
                _logger.LogWarning("No se pudieron obtener permisos para el sidebar: {Error}", errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar permisos para el sidebar");
        }

        return View(model);
    }

    private static void MostrarTodo(SidebarViewModel model)
    {
        model.PuedeVerDashboard = true;
        model.PuedeVerLineaTiempo = true;
        model.PuedeVerColaEspera = true;

        model.PuedeVerAdministracion = true;
        model.PuedeVerPerfiles = true;
        model.PuedeVerUsuarios = true;
        model.PuedeVerPermisos = true;
        model.PuedeVerSalas = true;

        model.PuedeVerCatalogos = true;
        model.PuedeVerPacientes = true;
        model.PuedeVerMedicamentos = true;
        model.PuedeVerClinicas = true;
        model.PuedeVerTiposCirugia = true;
        model.PuedeVerCirugias = true;
        model.PuedeVerTiposDiagnostico = true;
        model.PuedeVerDiagnosticos = true;
        model.PuedeVerTratamientos = true;
        model.PuedeVerRecomendaciones = true;
        model.PuedeVerExamenes = true;
        model.PuedeVerTiposAntecedente = true;
        model.PuedeVerTiposSignoVital = true;

        model.PuedeVerAgenda = true;
        model.PuedeVerExpedientes = true;
        model.PuedeVerReportes = true;
        model.PuedeVerAlertas = true;
    }

    private static void AplicarPermisos(SidebarViewModel model, HashSet<string> modulos)
    {
        // ── Navegación ──
        model.PuedeVerDashboard = modulos.Contains("dashboard");
        model.PuedeVerLineaTiempo = modulos.Contains("linea_tiempo");
        model.PuedeVerColaEspera = modulos.Contains("cola_espera");

        // ── Administración (sub-módulos) ──
        model.PuedeVerPerfiles = modulos.Contains("perfiles");
        model.PuedeVerUsuarios = modulos.Contains("usuarios");
        model.PuedeVerPermisos = modulos.Contains("permisos");
        model.PuedeVerSalas = modulos.Contains("salas");
        model.PuedeVerAdministracion = model.PuedeVerPerfiles || model.PuedeVerUsuarios
            || model.PuedeVerPermisos || model.PuedeVerSalas;

        // ── Catálogos (sub-módulos) ──
        model.PuedeVerPacientes = modulos.Contains("pacientes");
        model.PuedeVerMedicamentos = modulos.Contains("medicamentos");
        model.PuedeVerClinicas = modulos.Contains("clinicas");
        model.PuedeVerTiposCirugia = modulos.Contains("tipos_cirugia");
        model.PuedeVerCirugias = modulos.Contains("cirugias");
        model.PuedeVerTiposDiagnostico = modulos.Contains("tipos_dx");
        model.PuedeVerDiagnosticos = modulos.Contains("diagnosticos");
        model.PuedeVerTratamientos = modulos.Contains("tratamientos");
        model.PuedeVerRecomendaciones = modulos.Contains("recomendaciones");
        model.PuedeVerExamenes = modulos.Contains("examenes");
        model.PuedeVerTiposAntecedente = modulos.Contains("tipos_antecedente");
        model.PuedeVerTiposSignoVital = modulos.Contains("tipos_signo_vital");
        model.PuedeVerCatalogos = model.PuedeVerPacientes || model.PuedeVerMedicamentos
            || model.PuedeVerClinicas || model.PuedeVerTiposCirugia || model.PuedeVerCirugias
            || model.PuedeVerTiposDiagnostico || model.PuedeVerDiagnosticos
            || model.PuedeVerTratamientos || model.PuedeVerRecomendaciones || model.PuedeVerExamenes
            || model.PuedeVerTiposAntecedente || model.PuedeVerTiposSignoVital;

        // ── Módulos individuales ──
        model.PuedeVerAgenda = modulos.Contains("agenda");
        model.PuedeVerExpedientes = modulos.Contains("expedientes");
        model.PuedeVerReportes = modulos.Contains("reportes");
        model.PuedeVerAlertas = modulos.Contains("alertas");
    }
}

/// <summary>
/// Modelo de datos para la vista del sidebar colapsable.
/// </summary>
public class SidebarViewModel
{
    // ── Navegación (links simples) ──
    public bool PuedeVerDashboard { get; set; }
    public bool PuedeVerLineaTiempo { get; set; }
    public bool PuedeVerColaEspera { get; set; }

    // ── Administración (colapsable) ──
    public bool PuedeVerAdministracion { get; set; }
    public bool PuedeVerPerfiles { get; set; }
    public bool PuedeVerUsuarios { get; set; }
    public bool PuedeVerPermisos { get; set; }
    public bool PuedeVerSalas { get; set; }

    // ── Catálogos (colapsable) ──
    public bool PuedeVerCatalogos { get; set; }
    public bool PuedeVerPacientes { get; set; }
    public bool PuedeVerMedicamentos { get; set; }
    public bool PuedeVerClinicas { get; set; }
    public bool PuedeVerTiposCirugia { get; set; }
    public bool PuedeVerCirugias { get; set; }
    public bool PuedeVerTiposDiagnostico { get; set; }
    public bool PuedeVerDiagnosticos { get; set; }
    public bool PuedeVerTratamientos { get; set; }
    public bool PuedeVerRecomendaciones { get; set; }
    public bool PuedeVerExamenes { get; set; }
    public bool PuedeVerTiposAntecedente { get; set; }
    public bool PuedeVerTiposSignoVital { get; set; }

    // ── Módulos individuales ──
    public bool PuedeVerAgenda { get; set; }
    public bool PuedeVerExpedientes { get; set; }
    public bool PuedeVerReportes { get; set; }
    public bool PuedeVerAlertas { get; set; }

    // ── Contexto de navegación actual ──
    public string CurrentArea { get; set; } = string.Empty;
    public string CurrentController { get; set; } = string.Empty;
}
