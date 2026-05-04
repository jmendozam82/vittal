# Controller — Permission System

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para configurar el sistema de permisos granulares.
> **Prerequisito:** skills/controller/SKILL.md

---

## RequirePermissionAttribute

```csharp
// src/Vittal.API/Attributes/RequirePermissionAttribute.cs
namespace Vittal.API.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute
{
    public string ModuloClave { get; }
    public PermissionType Tipo { get; }

    public RequirePermissionAttribute(string moduloClave, PermissionType tipo)
    {
        ModuloClave = moduloClave;
        Tipo = tipo;
    }
}

public enum PermissionType { Read, Create, Update }
```

---

## PermissionFilter

```csharp
// src/Vittal.API/Filters/PermissionFilter.cs
public class PermissionFilter : IAsyncActionFilter
{
    private readonly IPermisoService _permisoService;

    public PermissionFilter(IPermisoService permisoService)
    {
        _permisoService = permisoService;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor
            .EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .FirstOrDefault();

        if (attribute is null) { await next(); return; }

        var user = context.HttpContext.User;

        // Admins tienen acceso total
        if (user.EsAdmin()) { await next(); return; }

        var usuarioId = user.GetUsuarioId();
        var clinicaId = user.GetClinicaId();

        var permiso = await _permisoService
            .GetPermisoPorUsuarioYModuloAsync(usuarioId, clinicaId, attribute.ModuloClave);

        if (permiso is null || !TienePermiso(permiso, attribute.Tipo))
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail("No tiene permisos para realizar esta operación."))
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }

    private static bool TienePermiso(PermisoUsuarioDto permiso, PermissionType tipo)
        => tipo switch
        {
            PermissionType.Read   => permiso.PuedeLeer,
            PermissionType.Create => permiso.PuedeCrear,
            PermissionType.Update => permiso.PuedeActualizar,
            _ => false
        };
}
```

---

## TenantMiddleware

```csharp
// src/Vittal.API/Middleware/TenantMiddleware.cs
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUsuarioRepository usuarioRepo)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        { await _next(context); return; }

        var authUserIdStr = context.User.FindFirstValue("sub");
        if (!Guid.TryParse(authUserIdStr, out var authUserId))
        { await _next(context); return; }

        try
        {
            var usuario = await usuarioRepo.GetByAuthUserIdAsync(authUserId);
            if (usuario is not null)
            {
                var claims = new List<Claim>
                {
                    new("app_clinica_id", usuario.ClinicaId.ToString()),
                    new("app_usuario_id", usuario.Id.ToString()),
                    new("app_perfil_id",  usuario.PerfilId.ToString()),
                    new("app_es_admin",   usuario.EsAdmin.ToString().ToLower())
                };
                context.User.AddIdentity(new ClaimsIdentity(claims));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TenantMiddleware para auth_user_id {AuthUserId}", authUserId);
        }

        await _next(context);
    }
}
```

---

## Checklist de Calidad — Permissions

- [ ] RequirePermissionAttribute en cada endpoint (excepto Login)
- [ ] PermissionFilter registrado globalmente en Program.cs
- [ ] Admins bypassean verificación de permisos
- [ ] TenantMiddleware ejecutado DESPUÉS de Authentication
- [ ] Claims injectados: app_clinica_id, app_usuario_id, app_perfil_id, app_es_admin
- [ ] PermissionType no incluye Delete
- [ ] 403 retornado cuando no hay permiso

---

*skills/controller/permission.md — Vittal v1.0.0*
