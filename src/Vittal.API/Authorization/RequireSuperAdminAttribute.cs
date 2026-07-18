using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vittal.API.Extensions;

namespace Vittal.API.Authorization;

/// <summary>
/// Attribute que restringe el acceso exclusivamente a usuarios
/// con el flag es_super_admin = true (Super Admin Global).
/// Usado en endpoints de provisionamiento de clínicas y
/// administración global del sistema.
/// Historia de Usuario: HU-SA01 — Super Admin Global
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireSuperAdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!user.EsSuperAdmin())
        {
            context.Result = new ForbidResult();
        }

        await Task.CompletedTask;
    }
}
