using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vittal.API.Extensions;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
namespace Vittal.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var authUserId = context.User.GetAuthUserId();

            if (authUserId != Guid.Empty)
            {
                using var scope = context.RequestServices.CreateScope();
                var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();

                var result = await usuarioService.GetByAuthUserIdAsync(authUserId);

                if (result.IsSuccess && result.Data != null)
                {
                    var appIdentity = new ClaimsIdentity(new[]
                    {
                        new Claim("app_usuario_id", result.Data.UsuarioId.ToString()),
                        new Claim("app_clinica_id", result.Data.ClinicaId.ToString()),
                        new Claim("app_es_admin", result.Data.EsAdmin.ToString()),
                        new Claim("app_es_super_admin", result.Data.EsSuperAdmin.ToString()),
                        new Claim("app_es_doctor", result.Data.EsDoctor.ToString()),
                        new Claim("app_perfil_id", result.Data.PerfilId.ToString())
                    });

                    context.User.AddIdentity(appIdentity);

                    // Determinar el clinica_id efectivo para el tenant context
                    var effectiveClinicaId = result.Data.ClinicaId;

                    // Si el usuario es Super Admin y envía header X-Clinica-Override, usar ese clinica_id
                    var overrideHeader = context.Request.Headers["X-Clinica-Override"].FirstOrDefault();
                    if (result.Data.EsSuperAdmin && !string.IsNullOrEmpty(overrideHeader) && Guid.TryParse(overrideHeader, out var parsedOverrideId))
                    {
                        effectiveClinicaId = parsedOverrideId;

                        // Agregar claim de override para que los controllers puedan detectarlo
                        context.User.AddIdentity(new ClaimsIdentity(new[]
                        {
                            new Claim("app_clinica_override", effectiveClinicaId.ToString())
                        }));
                    }

                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { Success = false, Message = "Usuario no autorizado o inactivo en el sistema" });
                    return;
                }
            }
        }

        await _next(context);
    }
}
