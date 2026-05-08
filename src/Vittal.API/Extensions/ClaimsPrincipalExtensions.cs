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

    public static Guid GetClinicaId(this ClaimsPrincipal user)
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

    public static Guid GetInternalPerfilId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("app_perfil_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
