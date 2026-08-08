using System;
using System.Security.Claims;

namespace Vittal.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetAuthUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Obtiene el clinica_id efectivo. Si el usuario es Super Admin y tiene
    /// un override activo (header X-Clinica-Override), usa ese.
    /// Si no, usa el clinica_id del JWT.
    /// </summary>
    public static Guid GetClinicaId(this ClaimsPrincipal user)
    {
        // 1. Si hay override activo (Super Admin que cambió de clínica), usarlo
        var overrideClaim = user.FindFirst("app_clinica_override");
        if (overrideClaim != null && Guid.TryParse(overrideClaim.Value, out var overrideId))
            return overrideId;

        // 2. Si no, usar la clínica del claim original
        var claim = user.FindFirst("app_clinica_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Obtiene el clinica_id del claim original (sin override).
    /// Útil para operaciones que siempre deben usar la clínica del usuario,
    /// como el login o consultas del perfil propio.
    /// </summary>
    public static Guid GetOriginalClinicaId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_clinica_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    public static Guid GetInternalUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_usuario_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    public static bool EsAdmin(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_es_admin");
        return claim != null && bool.TryParse(claim.Value, out var isAdmin) && isAdmin;
    }

    public static bool EsSuperAdmin(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_es_super_admin");
        return claim != null && bool.TryParse(claim.Value, out var isSuper) && isSuper;
    }

    /// <summary>
    /// Indica si el usuario autenticado tiene perfil de doctor (es_doctor = true).
    /// Los doctores solo ven/operan sobre sus propios pacientes y citas.
    /// </summary>
    public static bool EsDoctor(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_es_doctor");
        return claim != null && bool.TryParse(claim.Value, out var esDoctor) && esDoctor;
    }

    public static Guid GetInternalPerfilId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_perfil_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
