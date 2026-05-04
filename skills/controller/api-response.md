# Controller — API Response & Extensions

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para configurar wrappers de respuesta y extensiones JWT.
> **Prerequisito:** skills/controller/SKILL.md

---

## ApiResponse<T> — Wrapper

```csharp
// src/Vittal.API/Models/ApiResponse.cs
namespace Vittal.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Created(T data, string message = "Registro creado exitosamente.")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors?.ToList() ?? new() };
}
```

---

## ClaimsPrincipalExtensions

```csharp
// src/Vittal.API/Extensions/ClaimsPrincipalExtensions.cs
namespace Vittal.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string ClinicaIdClaim = "app_clinica_id";
    private const string UsuarioIdClaim = "app_usuario_id";
    private const string PerfilIdClaim  = "app_perfil_id";
    private const string EsAdminClaim   = "app_es_admin";

    public static Guid GetClinicaId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClinicaIdClaim)
            ?? throw new InvalidOperationException(
                "Claim 'app_clinica_id' no encontrado. Verificar TenantMiddleware.");
        return Guid.Parse(claim);
    }

    public static Guid GetUsuarioId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(UsuarioIdClaim)
            ?? throw new InvalidOperationException("Claim 'app_usuario_id' no encontrado.");
        return Guid.Parse(claim);
    }

    public static Guid GetPerfilId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(PerfilIdClaim)
            ?? throw new InvalidOperationException("Claim 'app_perfil_id' no encontrado.");
        return Guid.Parse(claim);
    }

    public static bool EsAdmin(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(EsAdminClaim);
        return bool.TryParse(claim, out var esAdmin) && esAdmin;
    }
}
```

---

## ServiceResultExtensions

```csharp
// src/Vittal.API/Extensions/ServiceResultExtensions.cs
namespace Vittal.API.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ServiceResult<T> result, ControllerBase controller)
    {
        if (result.Success)
        {
            var response = ApiResponse<T>.Ok(result.Data!, result.Message);
            return controller.Ok(response);
        }

        var errorResponse = ApiResponse<T>.Fail(result.Message, result.Errors);

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound        => controller.NotFound(errorResponse),
            ServiceErrorType.ValidationError => controller.BadRequest(errorResponse),
            ServiceErrorType.Duplicate       => controller.Conflict(errorResponse),
            ServiceErrorType.Unauthorized    => controller.StatusCode(403, errorResponse),
            ServiceErrorType.BusinessError   => controller.UnprocessableEntity(errorResponse),
            ServiceErrorType.ServerError     => controller.StatusCode(500, errorResponse),
            _ => controller.StatusCode(500, errorResponse)
        };
    }

    public static IActionResult ToCreatedResult<T>(
        this ServiceResult<T> result, ControllerBase controller,
        string actionName, object routeValues)
    {
        if (result.Success)
        {
            var response = ApiResponse<T>.Created(result.Data!, result.Message);
            return controller.CreatedAtAction(actionName, routeValues, response);
        }
        return result.ToActionResult(controller);
    }
}
```

---

## Checklist de Calidad — Response & Extensions

- [ ] ApiResponse<T> usa `Success` (no `IsSuccess`)
- [ ] ClaimsPrincipalExtensions lanza InvalidOperationException si claim falta
- [ ] ServiceErrorType mapea correctamente a códigos HTTP
- [ ] `ToCreatedResult` retorna 201 con Location header
- [ ] EsAdmin usa `bool.TryParse` para safe parsing
- [ ] Timestamp siempre en UTC (`DateTime.UtcNow`)

---

*skills/controller/api-response.md — Vittal v1.0.0*
