using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vittal.API.Extensions;
using Vittal.BLL.Services;

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
                // Resolve scoped service
                using var scope = context.RequestServices.CreateScope();
                var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
                
                var result = await usuarioService.GetByAuthUserIdAsync(authUserId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var appIdentity = new ClaimsIdentity(new[]
                    {
                        new Claim("app_usuario_id", result.Data.UsuarioId.ToString()),
                        new Claim("app_clinica_id", result.Data.ClinicaId.ToString()),
                        new Claim("app_es_admin", result.Data.EsAdmin.ToString())
                    });

                    context.User.AddIdentity(appIdentity);
                }
                else
                {
                    // Si el usuario no existe en la base de datos interna, cerramos sesión/devolvemos 401
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { Success = false, Message = "Usuario no autorizado o inactivo en el sistema" });
                    return;
                }
            }
        }

        await _next(context);
    }
}
