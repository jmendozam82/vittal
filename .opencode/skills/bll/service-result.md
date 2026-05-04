# BLL — ServiceResult & Exceptions

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para implementar el patrón de resultado de servicios.
> **Prerequisito:** skills/bll/SKILL.md

---

## ServiceResult<T> — Wrapper de Resultado

```csharp
// src/Vittal.BLL/Common/ServiceResult.cs
namespace Vittal.BLL.Common;

public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = new();
    public ServiceErrorType ErrorType { get; private set; } = ServiceErrorType.None;

    public static ServiceResult<T> Ok(T data, string message = "")
        => new() { Success = true, Data = data, Message = message };

    public static ServiceResult<T> Created(T data, string message = "Registro creado exitosamente.")
        => new() { Success = true, Data = data, Message = message };

    public static ServiceResult<T> NotFound(string message = "El registro no fue encontrado.")
        => new() { Success = false, Message = message, ErrorType = ServiceErrorType.NotFound };

    public static ServiceResult<T> ValidationError(IEnumerable<string> errors)
        => new() { Success = false, Message = "Los datos ingresados no son válidos.",
                   Errors = errors.ToList(), ErrorType = ServiceErrorType.ValidationError };

    public static ServiceResult<T> ValidationError(string error)
        => ValidationError(new[] { error });

    public static ServiceResult<T> BusinessError(string message)
        => new() { Success = false, Message = message, ErrorType = ServiceErrorType.BusinessError };

    public static ServiceResult<T> Unauthorized(string message = "No tiene permisos para esta operación.")
        => new() { Success = false, Message = message, ErrorType = ServiceErrorType.Unauthorized };

    public static ServiceResult<T> Duplicate(string message)
        => new() { Success = false, Message = message, ErrorType = ServiceErrorType.Duplicate };

    public static ServiceResult<T> ServerError(string message = "Ocurrió un error inesperado.")
        => new() { Success = false, Message = message, ErrorType = ServiceErrorType.ServerError };
}

public enum ServiceErrorType
{
    None,
    NotFound,           // → 404
    ValidationError,    // → 400
    BusinessError,      // → 422
    Unauthorized,       // → 403
    Duplicate,          // → 409
    ServerError         // → 500
}
```

---

## PagedResult<T>

```csharp
// src/Vittal.BLL/Common/PagedResult.cs
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

---

## Excepciones de Dominio

```csharp
// src/Vittal.BLL/Exceptions/BusinessException.cs
namespace Vittal.BLL.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

// src/Vittal.BLL/Exceptions/NotFoundException.cs
public class NotFoundException : BusinessException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public NotFoundException(string entityName, object entityId)
        : base($"{entityName} con ID '{entityId}' no fue encontrado.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
```

---

## Checklist de Calidad — ServiceResult

- [ ] Toda operación retorna `ServiceResult<T>` — nunca lanza excepciones al Controller
- [ ] `GetAllAsync` retorna `Ok(IEnumerable<TDto>)`
- [ ] `GetByIdAsync` retorna `NotFound` si no existe o es de otro tenant
- [ ] `CreateAsync` retorna `Created` con DTO persistido
- [ ] `UpdateAsync` verifica existencia antes de actualizar
- [ ] `DeactivateAsync` verifica existencia y retorna `Ok(true)`
- [ ] Errores de validación usan `ValidationError` con lista de mensajes
- [ ] Duplicados usan `Duplicate` → 409
- [ ] Errores inesperados usan `ServerError` con logging

---

*skills/bll/service-result.md — Vittal v1.0.0*
