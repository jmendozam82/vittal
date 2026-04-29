# skill-bll.md — Skill: Business Logic Layer (BLL)

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar este skill:** Antes de implementar cualquier Service,
> validador FluentValidation o lógica de negocio en el proyecto Vittal.
> **Prerequisito:** Haber leído CLAUDE.md, skill-supabase.md y skill-dal.md.
> Las interfaces del Repository (DAL) deben existir antes de implementar el Service.

---

## 1. Principios Fundamentales del BLL

```
1. El BLL es el único lugar donde vive la lógica de negocio — ni el DAL
   ni los Controllers toman decisiones de negocio
2. El BLL NUNCA retorna Entities — siempre transforma a DTOs antes de retornar
3. El BLL NUNCA llama directamente a la BD — usa interfaces del DAL (I*Repository)
4. Toda operación de escritura extrae clinicaId del parámetro — nunca lo asume
5. Las validaciones van en el BLL con FluentValidation — no en el Controller
6. El BLL captura excepciones del DAL y las convierte en ServiceResult<T>
7. Un Service por entidad/módulo principal — mantener cohesión alta
8. Las operaciones que modifican múltiples entidades en una misma acción
   de negocio coordinan los Repositories desde el Service
9. El Service es el responsable de verificar permisos de negocio adicionales
   más allá del JWT (ej: un doctor solo edita sus propios pacientes)
10. Async/await en todos los métodos — no operaciones síncronas
```

---

## 2. Estructura del Proyecto Vittal.BLL

```
src/Vittal.BLL/
├── Common/
│   ├── ServiceResult.cs            ← Wrapper de resultado de toda operación BLL
│   └── PagedResult.cs              ← Resultado paginado para listados grandes
├── Exceptions/
│   ├── BusinessException.cs        ← Excepción base de regla de negocio violada
│   ├── NotFoundException.cs        ← Registro no encontrado
│   └── UnauthorizedAccessException.cs ← Acceso no permitido por regla de negocio
├── Interfaces/
│   ├── IClinicaService.cs
│   ├── IPerfilService.cs
│   ├── IUsuarioService.cs
│   ├── IPermisoService.cs
│   ├── ISalaService.cs
│   ├── IPacienteService.cs
│   ├── IMedicamentoService.cs
│   ├── ITipoCirugiaService.cs
│   ├── ICirugiaService.cs
│   ├── ITipoDiagnosticoService.cs
│   ├── IDiagnosticoService.cs
│   ├── ITratamientoService.cs
│   ├── IRecomendacionService.cs
│   ├── IExamenService.cs
│   ├── ICitaService.cs
│   ├── IExpedienteService.cs
│   └── IAlertaEsperaService.cs
├── Services/
│   ├── ClinicaService.cs
│   ├── PerfilService.cs
│   ├── UsuarioService.cs
│   ├── PermisoService.cs
│   ├── SalaService.cs
│   ├── PacienteService.cs
│   ├── MedicamentoService.cs
│   ├── TipoCirugiaService.cs
│   ├── CirugiaService.cs
│   ├── TipoDiagnosticoService.cs
│   ├── DiagnosticoService.cs
│   ├── TratamientoService.cs
│   ├── RecomendacionService.cs
│   ├── ExamenService.cs
│   ├── CitaService.cs
│   ├── ExpedienteService.cs
│   └── AlertaEsperaService.cs
└── Validators/
    ├── PacienteRequestValidator.cs
    ├── CitaRequestValidator.cs
    ├── UsuarioRequestValidator.cs
    ├── ExpedienteRequestValidator.cs
    └── [Un validador por cada DTO de Request]
```

---

## 3. NuGet Packages Requeridos

```xml
<!-- src/Vittal.BLL/Vittal.BLL.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Validación declarativa de DTOs -->
    <PackageReference Include="FluentValidation" Version="11.9.2" />
    <!-- Integración con DI de .NET para registrar validadores automáticamente -->
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.2" />
    <!-- Mapeo automático Entity → DTO y viceversa -->
    <PackageReference Include="AutoMapper" Version="13.0.1" />
    <!-- Integración AutoMapper con DI de .NET -->
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />

    <!-- Referencias a proyectos internos -->
    <ProjectReference Include="..\Vittal.DAL\Vittal.DAL.csproj" />
    <ProjectReference Include="..\Vittal.DTO\Vittal.DTO.csproj" />
    <ProjectReference Include="..\Vittal.Entity\Vittal.Entity.csproj" />
  </ItemGroup>
</Project>
```

---

## 4. ServiceResult — Wrapper de Resultado

Toda operación del BLL retorna `ServiceResult<T>`. Nunca lanzar excepciones
directamente hacia el Controller — encapsularlas en el resultado.

```csharp
// src/Vittal.BLL/Common/ServiceResult.cs
namespace Vittal.BLL.Common;

/// <summary>
/// Wrapper de resultado estándar para todas las operaciones del BLL.
/// El Controller inspecciona Success para determinar el código HTTP a retornar.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = new();
    public ServiceErrorType ErrorType { get; private set; } = ServiceErrorType.None;

    // ── Factory methods ──────────────────────────────────────────────────

    public static ServiceResult<T> Ok(T data, string message = "")
        => new() { Success = true, Data = data, Message = message };

    public static ServiceResult<T> Created(T data, string message = "Registro creado exitosamente.")
        => new() { Success = true, Data = data, Message = message };

    public static ServiceResult<T> NotFound(string message = "El registro no fue encontrado.")
        => new() { Success = false, Message = message, ErrorType = ServiceErrorType.NotFound };

    public static ServiceResult<T> ValidationError(IEnumerable<string> errors)
        => new()
        {
            Success = false,
            Message = "Los datos ingresados no son válidos.",
            Errors = errors.ToList(),
            ErrorType = ServiceErrorType.ValidationError
        };

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

/// <summary>
/// Tipo de error para que el Controller pueda mapear al código HTTP correcto.
/// </summary>
public enum ServiceErrorType
{
    None,
    NotFound,           // → 404
    ValidationError,    // → 400
    BusinessError,      // → 422 Unprocessable Entity
    Unauthorized,       // → 403
    Duplicate,          // → 409 Conflict
    ServerError         // → 500
}

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

## 5. Excepciones de Dominio del BLL

```csharp
// src/Vittal.BLL/Exceptions/BusinessException.cs
namespace Vittal.BLL.Exceptions;

/// <summary>Excepción base para violaciones de reglas de negocio.</summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

// src/Vittal.BLL/Exceptions/NotFoundException.cs
/// <summary>Registro solicitado no existe o no pertenece al tenant.</summary>
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

## 6. Plantilla Maestra de Interfaz de Service

```csharp
// src/Vittal.BLL/Interfaces/I[Entidad]Service.cs
namespace Vittal.BLL.Interfaces;

/// <summary>
/// Contrato de negocio para la entidad [Entidad].
/// Historia de Usuario: HU[XX] — [Nombre de la HU]
/// Siempre retorna ServiceResult<T> — nunca lanza excepciones al Controller.
/// </summary>
public interface I[Entidad]Service
{
    // ── Consultas ────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todos los registros activos de la clínica.
    /// </summary>
    Task<ServiceResult<IEnumerable<[Entidad]ResponseDto>>> GetAllAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene un registro por ID. Retorna NotFound si no existe en la clínica.
    /// </summary>
    Task<ServiceResult<[Entidad]ResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);

    // ── Comandos ─────────────────────────────────────────────────────────

    /// <summary>
    /// Valida y crea un nuevo registro.
    /// Retorna Created con el DTO del registro creado, o ValidationError/Duplicate.
    /// </summary>
    Task<ServiceResult<[Entidad]ResponseDto>> CreateAsync(
        [Entidad]RequestDto dto, Guid clinicaId);

    /// <summary>
    /// Valida y actualiza un registro existente.
    /// Retorna Ok con el DTO actualizado, o NotFound/ValidationError.
    /// </summary>
    Task<ServiceResult<[Entidad]ResponseDto>> UpdateAsync(
        Guid id, [Entidad]RequestDto dto, Guid clinicaId);

    /// <summary>
    /// Desactiva un registro (activo = false). NUNCA elimina.
    /// Retorna Ok si se desactivó, o NotFound si no existe.
    /// </summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
}
```

---

## 7. Plantilla Maestra de Implementación de Service

```csharp
// src/Vittal.BLL/Services/[Entidad]Service.cs
namespace Vittal.BLL.Services;

public class [Entidad]Service : I[Entidad]Service
{
    private readonly I[Entidad]Repository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<[Entidad]RequestDto> _validator;
    private readonly ILogger<[Entidad]Service> _logger;

    public [Entidad]Service(
        I[Entidad]Repository repository,
        IMapper mapper,
        IValidator<[Entidad]RequestDto> validator,
        ILogger<[Entidad]Service> logger)
    {
        _repository = repository;
        _mapper     = mapper;
        _validator  = validator;
        _logger     = logger;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<[Entidad]ResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            var entidades = await _repository.GetAllAsync(clinicaId);
            var dtos = _mapper.Map<IEnumerable<[Entidad]ResponseDto>>(entidades);
            return ServiceResult<IEnumerable<[Entidad]ResponseDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener [Entidad]s para clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<[Entidad]ResponseDto>>.ServerError();
        }
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────
    public async Task<ServiceResult<[Entidad]ResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            var entidad = await _repository.GetByIdAsync(id, clinicaId);

            if (entidad is null)
                return ServiceResult<[Entidad]ResponseDto>.NotFound(
                    "[Entidad] no encontrada o no pertenece a esta clínica.");

            return ServiceResult<[Entidad]ResponseDto>.Ok(
                _mapper.Map<[Entidad]ResponseDto>(entidad));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener [Entidad] {Id}", id);
            return ServiceResult<[Entidad]ResponseDto>.ServerError();
        }
    }

    // ── CreateAsync ──────────────────────────────────────────────────────
    public async Task<ServiceResult<[Entidad]ResponseDto>> CreateAsync(
        [Entidad]RequestDto dto, Guid clinicaId)
    {
        // 1. Validar con FluentValidation
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<[Entidad]ResponseDto>.ValidationError(
                validation.Errors.Select(e => e.ErrorMessage));

        // 2. Reglas de negocio adicionales (duplicados, etc.)
        // Ejemplo: verificar unicidad de email en la clínica
        // if (await _repository.ExistsAsync(clinicaId, "email", dto.Email))
        //     return ServiceResult<...>.Duplicate("Ya existe un registro con ese email.");

        try
        {
            // 3. Mapear DTO → Entity
            var entidad = _mapper.Map<[Entidad]>(dto);
            entidad.ClinicaId = clinicaId;  // Siempre asignar desde el JWT, no del DTO

            // 4. Persistir
            var id = await _repository.CreateAsync(entidad);

            // 5. Retornar el registro recién creado
            var created = await _repository.GetByIdAsync(id, clinicaId);
            return ServiceResult<[Entidad]ResponseDto>.Created(
                _mapper.Map<[Entidad]ResponseDto>(created!));
        }
        catch (DuplicateEntityException ex)
        {
            return ServiceResult<[Entidad]ResponseDto>.Duplicate(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear [Entidad] en clínica {ClinicaId}", clinicaId);
            return ServiceResult<[Entidad]ResponseDto>.ServerError();
        }
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────
    public async Task<ServiceResult<[Entidad]ResponseDto>> UpdateAsync(
        Guid id, [Entidad]RequestDto dto, Guid clinicaId)
    {
        // 1. Verificar que existe y pertenece a la clínica
        var existente = await _repository.GetByIdAsync(id, clinicaId);
        if (existente is null)
            return ServiceResult<[Entidad]ResponseDto>.NotFound();

        // 2. Validar con FluentValidation
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<[Entidad]ResponseDto>.ValidationError(
                validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            // 3. Actualizar propiedades — preservar clinicaId y campos de auditoría
            _mapper.Map(dto, existente);
            existente.Id = id;
            existente.ClinicaId = clinicaId;

            // 4. Persistir
            await _repository.UpdateAsync(existente);

            // 5. Retornar registro actualizado
            var updated = await _repository.GetByIdAsync(id, clinicaId);
            return ServiceResult<[Entidad]ResponseDto>.Ok(
                _mapper.Map<[Entidad]ResponseDto>(updated!),
                "Registro actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar [Entidad] {Id}", id);
            return ServiceResult<[Entidad]ResponseDto>.ServerError();
        }
    }

    // ── DeactivateAsync ──────────────────────────────────────────────────
    // REGLA ABSOLUTA: Desactivar, nunca eliminar
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        // Verificar que existe antes de intentar desactivar
        var existente = await _repository.GetByIdAsync(id, clinicaId);
        if (existente is null)
            return ServiceResult<bool>.NotFound();

        try
        {
            var result = await _repository.DeactivateAsync(id, clinicaId);
            return result
                ? ServiceResult<bool>.Ok(true, "Registro desactivado exitosamente.")
                : ServiceResult<bool>.BusinessError("No fue posible desactivar el registro.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar [Entidad] {Id}", id);
            return ServiceResult<bool>.ServerError();
        }
    }
}
```

---

## 8. Validadores FluentValidation

### 8.1 Plantilla maestra de validador

```csharp
// src/Vittal.BLL/Validators/[Entidad]RequestValidator.cs
namespace Vittal.BLL.Validators;

public class [Entidad]RequestValidator : AbstractValidator<[Entidad]RequestDto>
{
    public [Entidad]RequestValidator()
    {
        // ── Campos de texto obligatorios ─────────────────────────────────
        RuleFor(x => x.NombreCampo)
            .NotEmpty().WithMessage("El campo [Nombre] es obligatorio.")
            .MaximumLength(255).WithMessage("El campo [Nombre] no puede superar 255 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")
            .WithMessage("El campo [Nombre] solo permite letras y caracteres especiales del español.");

        // ── Email ────────────────────────────────────────────────────────
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(255).WithMessage("El correo no puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Email));  // Aplicar solo si se provee

        // ── Teléfono / Celular ───────────────────────────────────────────
        RuleFor(x => x.Celular)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .WithMessage("El número de celular no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        // ── Sexo ─────────────────────────────────────────────────────────
        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F")
            .WithMessage("El sexo debe ser 'M' (Masculino) o 'F' (Femenino).")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        // ── Fecha ────────────────────────────────────────────────────────
        RuleFor(x => x.FechaNacimiento)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha de nacimiento debe ser anterior a hoy.")
            .GreaterThan(new DateOnly(1900, 1, 1))
            .WithMessage("La fecha de nacimiento no es válida.")
            .When(x => x.FechaNacimiento.HasValue);

        // ── UUID / FK ────────────────────────────────────────────────────
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");
    }
}
```

### 8.2 PacienteRequestValidator (HU07)

```csharp
// src/Vittal.BLL/Validators/PacienteRequestValidator.cs
namespace Vittal.BLL.Validators;

public class PacienteRequestValidator : AbstractValidator<PacienteRequestDto>
{
    public PacienteRequestValidator()
    {
        RuleFor(x => x.PrimerNombre)
            .NotEmpty().WithMessage("El primer nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El primer nombre no puede superar 100 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")
            .WithMessage("El primer nombre solo permite letras.");

        RuleFor(x => x.PrimerApellido)
            .NotEmpty().WithMessage("El primer apellido es obligatorio.")
            .MaximumLength(100).WithMessage("El primer apellido no puede superar 100 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")
            .WithMessage("El primer apellido solo permite letras.");

        RuleFor(x => x.SegundoNombre)
            .MaximumLength(100).WithMessage("El segundo nombre no puede superar 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SegundoNombre));

        RuleFor(x => x.SegundoApellido)
            .MaximumLength(100).WithMessage("El segundo apellido no puede superar 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SegundoApellido));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(255).WithMessage("El correo no puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Celular)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .WithMessage("El número de celular no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F")
            .WithMessage("El sexo debe ser 'M' (Masculino) o 'F' (Femenino).")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.FechaNacimiento)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha de nacimiento debe ser anterior a hoy.")
            .GreaterThan(new DateOnly(1900, 1, 1))
            .WithMessage("La fecha de nacimiento no es válida.")
            .When(x => x.FechaNacimiento.HasValue);

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar el doctor responsable del paciente.");

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("La dirección no puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Direccion));
    }
}
```

### 8.3 CitaRequestValidator (HU21)

```csharp
// src/Vittal.BLL/Validators/CitaRequestValidator.cs
namespace Vittal.BLL.Validators;

public class CitaRequestValidator : AbstractValidator<CitaRequestDto>
{
    public CitaRequestValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("Debe seleccionar un paciente.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");

        RuleFor(x => x.FechaCita)
            .NotEmpty().WithMessage("La fecha de la cita es obligatoria.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha de la cita no puede ser en el pasado.");

        RuleFor(x => x.HoraCita)
            .NotEmpty().WithMessage("La hora de la cita es obligatoria.");

        RuleFor(x => x.Lugar)
            .NotEmpty().WithMessage("El lugar de la cita es obligatorio.")
            .MaximumLength(255).WithMessage("El lugar no puede superar 255 caracteres.");

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo no puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Motivo));
    }
}
```

### 8.4 UsuarioRequestValidator (HU04)

```csharp
// src/Vittal.BLL/Validators/UsuarioRequestValidator.cs
namespace Vittal.BLL.Validators;

public class UsuarioRequestValidator : AbstractValidator<UsuarioRequestDto>
{
    public UsuarioRequestValidator()
    {
        RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(4).WithMessage("El usuario debe tener al menos 4 caracteres.")
            .MaximumLength(100).WithMessage("El usuario no puede superar 100 caracteres.")
            .Matches(@"^[a-zA-Z0-9_\.\-]+$")
            .WithMessage("El usuario solo permite letras, números, puntos, guiones y guiones bajos.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede superar 100 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"[0-9]").WithMessage("La contraseña debe contener al menos un número.")
            .When(x => !string.IsNullOrEmpty(x.Contrasena));  // Opcional en edición

        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(255).WithMessage("Los nombres no pueden superar 255 caracteres.");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(255).WithMessage("Los apellidos no pueden superar 255 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(255).WithMessage("El correo no puede superar 255 caracteres.");

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F")
            .WithMessage("El sexo debe ser 'M' (Masculino) o 'F' (Femenino).")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.Celular)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .WithMessage("El número de celular no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("Debe seleccionar un perfil para el usuario.");
    }
}
```

---

## 9. Perfiles AutoMapper

```csharp
// src/Vittal.BLL/Mappings/VittalMappingProfile.cs
namespace Vittal.BLL.Mappings;

public class VittalMappingProfile : Profile
{
    public VittalMappingProfile()
    {
        // ── Paciente ─────────────────────────────────────────────────────
        CreateMap<Paciente, PacienteResponseDto>()
            .ForMember(dest => dest.NombreCompleto,
                opt => opt.MapFrom(src =>
                    $"{src.PrimerNombre} {src.SegundoNombre} {src.PrimerApellido} {src.SegundoApellido}"
                    .Replace("  ", " ").Trim()));

        CreateMap<PacienteRequestDto, Paciente>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())        // BD lo genera
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore()) // Viene del JWT
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Cita ─────────────────────────────────────────────────────────
        CreateMap<Cita, CitaResponseDto>();
        CreateMap<CitaRequestDto, Cita>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.Estado, opt => opt.Ignore())    // BLL asigna 'agendada'
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Usuario ──────────────────────────────────────────────────────
        CreateMap<Usuario, UsuarioResponseDto>()
            .ForMember(dest => dest.NombreCompleto,
                opt => opt.MapFrom(src => $"{src.Nombres} {src.Apellidos}".Trim()));
        CreateMap<UsuarioRequestDto, Usuario>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.AuthUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Perfil ───────────────────────────────────────────────────────
        CreateMap<Perfil, PerfilResponseDto>();
        CreateMap<PerfilRequestDto, Perfil>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Expediente ───────────────────────────────────────────────────
        CreateMap<Expediente, ExpedienteResponseDto>();
        CreateMap<HojaCita, HojaCitaResponseDto>();

        // ── Catálogos simples (patrón idéntico para todos) ────────────────
        // Medicamento, TipoCirugia, Cirugia, TipoDiagnostico,
        // Diagnostico, Tratamiento, Recomendacion, Examen, Sala
        foreach (var map in GetCatalogoMaps())
        {
            map.ForMember("Id", opt => opt.Ignore());
            map.ForMember("ClinicaId", opt => opt.Ignore());
            map.ForMember("Activo", opt => opt.Ignore());
            map.ForMember("FechaCreacion", opt => opt.Ignore());
            map.ForMember("FechaModificacion", opt => opt.Ignore());
        }
    }

    private IEnumerable<IMappingExpression> GetCatalogoMaps()
    {
        yield return CreateMap<Medicamento, MedicamentoResponseDto>();
        yield return CreateMap<MedicamentoRequestDto, Medicamento>();
        yield return CreateMap<TipoCirugia, TipoCirugiaResponseDto>();
        yield return CreateMap<TipoCirugiaRequestDto, TipoCirugia>();
        yield return CreateMap<Cirugia, CirugiaResponseDto>();
        yield return CreateMap<CirugiaRequestDto, Cirugia>();
        yield return CreateMap<TipoDiagnostico, TipoDiagnosticoResponseDto>();
        yield return CreateMap<TipoDiagnosticoRequestDto, TipoDiagnostico>();
        yield return CreateMap<Diagnostico, DiagnosticoResponseDto>();
        yield return CreateMap<DiagnosticoRequestDto, Diagnostico>();
        yield return CreateMap<Tratamiento, TratamientoResponseDto>();
        yield return CreateMap<TratamientoRequestDto, Tratamiento>();
        yield return CreateMap<Recomendacion, RecomendacionResponseDto>();
        yield return CreateMap<RecomendacionRequestDto, Recomendacion>();
        yield return CreateMap<Examen, ExamenResponseDto>();
        yield return CreateMap<ExamenRequestDto, Examen>();
        yield return CreateMap<Sala, SalaResponseDto>();
        yield return CreateMap<SalaRequestDto, Sala>();
    }
}
```

---

## 10. Services Implementados — Módulos Core

### 10.1 PacienteService (HU07)

```csharp
// src/Vittal.BLL/Services/PacienteService.cs
namespace Vittal.BLL.Services;

public class PacienteService : IPacienteService
{
    private readonly IPacienteRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<PacienteRequestDto> _validator;
    private readonly ILogger<PacienteService> _logger;

    public PacienteService(
        IPacienteRepository repository,
        IMapper mapper,
        IValidator<PacienteRequestDto> validator,
        ILogger<PacienteService> logger)
    {
        _repository = repository;
        _mapper     = mapper;
        _validator  = validator;
        _logger     = logger;
    }

    public async Task<ServiceResult<IEnumerable<PacienteResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            var pacientes = await _repository.GetAllAsync(clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Ok(
                _mapper.Map<IEnumerable<PacienteResponseDto>>(pacientes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes de clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.ServerError();
        }
    }

    public async Task<ServiceResult<IEnumerable<PacienteResponseDto>>> GetByDoctorAsync(
        Guid doctorId, Guid clinicaId)
    {
        try
        {
            var pacientes = await _repository.GetByDoctorAsync(doctorId, clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Ok(
                _mapper.Map<IEnumerable<PacienteResponseDto>>(pacientes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes del doctor {DoctorId}", doctorId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.ServerError();
        }
    }

    public async Task<ServiceResult<IEnumerable<PacienteResponseDto>>> SearchAsync(
        string termino, Guid clinicaId)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
            return ServiceResult<IEnumerable<PacienteResponseDto>>.ValidationError(
                "Ingrese al menos 2 caracteres para buscar.");

        try
        {
            var pacientes = await _repository.SearchAsync(termino, clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Ok(
                _mapper.Map<IEnumerable<PacienteResponseDto>>(pacientes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en búsqueda de pacientes: {Termino}", termino);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.ServerError();
        }
    }

    public async Task<ServiceResult<PacienteResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            var paciente = await _repository.GetByIdAsync(id, clinicaId);
            if (paciente is null)
                return ServiceResult<PacienteResponseDto>.NotFound(
                    "Paciente no encontrado en esta clínica.");

            return ServiceResult<PacienteResponseDto>.Ok(
                _mapper.Map<PacienteResponseDto>(paciente));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener paciente {Id}", id);
            return ServiceResult<PacienteResponseDto>.ServerError();
        }
    }

    public async Task<ServiceResult<PacienteResponseDto>> CreateAsync(
        PacienteRequestDto dto, Guid clinicaId)
    {
        // 1. Validar formato y campos requeridos
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<PacienteResponseDto>.ValidationError(
                validation.Errors.Select(e => e.ErrorMessage));

        // 2. Regla de negocio: email único por clínica si se provee
        if (!string.IsNullOrEmpty(dto.Email))
        {
            var emailExiste = await _repository.ExistsAsync(clinicaId, "email", dto.Email);
            if (emailExiste)
                return ServiceResult<PacienteResponseDto>.Duplicate(
                    "Ya existe un paciente con ese correo electrónico en esta clínica.");
        }

        try
        {
            var paciente = _mapper.Map<Paciente>(dto);
            paciente.ClinicaId = clinicaId;  // Siempre del JWT — nunca del DTO

            var id = await _repository.CreateAsync(paciente);
            var created = await _repository.GetByIdAsync(id, clinicaId);

            return ServiceResult<PacienteResponseDto>.Created(
                _mapper.Map<PacienteResponseDto>(created!));
        }
        catch (DuplicateEntityException ex)
        {
            return ServiceResult<PacienteResponseDto>.Duplicate(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear paciente en clínica {ClinicaId}", clinicaId);
            return ServiceResult<PacienteResponseDto>.ServerError();
        }
    }

    public async Task<ServiceResult<PacienteResponseDto>> UpdateAsync(
        Guid id, PacienteRequestDto dto, Guid clinicaId)
    {
        var existente = await _repository.GetByIdAsync(id, clinicaId);
        if (existente is null)
            return ServiceResult<PacienteResponseDto>.NotFound(
                "Paciente no encontrado en esta clínica.");

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<PacienteResponseDto>.ValidationError(
                validation.Errors.Select(e => e.ErrorMessage));

        // Verificar unicidad de email excluyendo el registro actual
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != existente.Email)
        {
            var emailExiste = await _repository.ExistsAsync(clinicaId, "email", dto.Email, id);
            if (emailExiste)
                return ServiceResult<PacienteResponseDto>.Duplicate(
                    "Ya existe un paciente con ese correo electrónico en esta clínica.");
        }

        try
        {
            _mapper.Map(dto, existente);
            existente.Id = id;
            existente.ClinicaId = clinicaId;

            await _repository.UpdateAsync(existente);
            var updated = await _repository.GetByIdAsync(id, clinicaId);

            return ServiceResult<PacienteResponseDto>.Ok(
                _mapper.Map<PacienteResponseDto>(updated!),
                "Paciente actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar paciente {Id}", id);
            return ServiceResult<PacienteResponseDto>.ServerError();
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        var existente = await _repository.GetByIdAsync(id, clinicaId);
        if (existente is null)
            return ServiceResult<bool>.NotFound("Paciente no encontrado en esta clínica.");

        // Regla de negocio: verificar si el paciente tiene citas pendientes
        // antes de desactivar (opcional — depende de requisito del cliente)

        try
        {
            var result = await _repository.DeactivateAsync(id, clinicaId);
            return result
                ? ServiceResult<bool>.Ok(true, "Paciente desactivado exitosamente.")
                : ServiceResult<bool>.BusinessError("No fue posible desactivar el paciente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar paciente {Id}", id);
            return ServiceResult<bool>.ServerError();
        }
    }
}
```

### 10.2 CitaService (HU21 + HU18)

```csharp
// src/Vittal.BLL/Services/CitaService.cs
namespace Vittal.BLL.Services;

public class CitaService : ICitaService
{
    private readonly ICitaRepository _citaRepository;
    private readonly IPacienteRepository _pacienteRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CitaRequestDto> _validator;
    private readonly ILogger<CitaService> _logger;

    public CitaService(
        ICitaRepository citaRepository,
        IPacienteRepository pacienteRepository,
        IMapper mapper,
        IValidator<CitaRequestDto> validator,
        ILogger<CitaService> logger)
    {
        _citaRepository    = citaRepository;
        _pacienteRepository = pacienteRepository;
        _mapper            = mapper;
        _validator         = validator;
        _logger            = logger;
    }

    public async Task<ServiceResult<IEnumerable<CitaResponseDto>>> GetColaEsperaAsync(
        Guid clinicaId, Guid? doctorId)
    {
        try
        {
            var citas = await _citaRepository.GetColaEsperaAsync(clinicaId, doctorId);
            return ServiceResult<IEnumerable<CitaResponseDto>>.Ok(
                _mapper.Map<IEnumerable<CitaResponseDto>>(citas));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cola de espera para clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<CitaResponseDto>>.ServerError();
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> CreateAsync(
        CitaRequestDto dto, Guid clinicaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<CitaResponseDto>.ValidationError(
                validation.Errors.Select(e => e.ErrorMessage));

        // Regla de negocio: verificar que el paciente pertenece a la clínica
        var paciente = await _pacienteRepository.GetByIdAsync(dto.PacienteId, clinicaId);
        if (paciente is null)
            return ServiceResult<CitaResponseDto>.ValidationError(
                "El paciente seleccionado no existe en esta clínica.");

        // Regla de negocio: verificar disponibilidad del doctor en esa fecha/hora
        var citasDelDia = await _citaRepository.GetByDoctorAndFechaAsync(
            dto.DoctorId, clinicaId, dto.FechaCita);

        var hayConflicto = citasDelDia.Any(c =>
            c.HoraCita == dto.HoraCita &&
            c.Estado != "cancelada");

        if (hayConflicto)
            return ServiceResult<CitaResponseDto>.BusinessError(
                "El doctor ya tiene una cita programada para esa fecha y hora.");

        try
        {
            var cita = _mapper.Map<Cita>(dto);
            cita.ClinicaId = clinicaId;
            cita.Estado    = "agendada";  // Estado inicial siempre es agendada

            var id = await _citaRepository.CreateAsync(cita);

            // Retornar la cita creada — el repositorio puede retornar la cita completa
            return ServiceResult<CitaResponseDto>.Created(
                new CitaResponseDto { Id = id, Estado = "agendada" },
                "Cita agendada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cita en clínica {ClinicaId}", clinicaId);
            return ServiceResult<CitaResponseDto>.ServerError();
        }
    }

    public async Task<ServiceResult<bool>> AtenderPacienteAsync(
        Guid citaId, Guid clinicaId)
    {
        // Regla de negocio del botón "Atender" en Cola de Espera
        // Cambia estado a 'en_atencion' y saca al paciente de la cola
        try
        {
            var result = await _citaRepository.CambiarEstadoAsync(
                citaId, clinicaId, "en_atencion");

            return result
                ? ServiceResult<bool>.Ok(true, "Paciente marcado como en atención.")
                : ServiceResult<bool>.NotFound("Cita no encontrada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al atender paciente de cita {CitaId}", citaId);
            return ServiceResult<bool>.ServerError();
        }
    }

    public async Task<ServiceResult<bool>> RegistrarLlegadaAsync(
        Guid citaId, Guid clinicaId)
    {
        // Registra la hora de llegada del paciente → estado 'en_espera'
        try
        {
            var horaLlegada = TimeOnly.FromDateTime(DateTime.Now);
            var result = await _citaRepository.CambiarEstadoAsync(
                citaId, clinicaId, "en_espera", horaLlegada);

            return result
                ? ServiceResult<bool>.Ok(true, "Llegada del paciente registrada.")
                : ServiceResult<bool>.NotFound("Cita no encontrada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar llegada de cita {CitaId}", citaId);
            return ServiceResult<bool>.ServerError();
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            // Cancelar = desactivar con estado 'cancelada'
            var result = await _citaRepository.DeactivateAsync(id, clinicaId);
            return result
                ? ServiceResult<bool>.Ok(true, "Cita cancelada exitosamente.")
                : ServiceResult<bool>.NotFound("Cita no encontrada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar cita {Id}", id);
            return ServiceResult<bool>.ServerError();
        }
    }
}
```

---

## 11. Registro en IOC (Vittal.IOC)

```csharp
// src/Vittal.IOC/DependencyInjection.cs — sección BLL
public static IServiceCollection AddVittalBLL(this IServiceCollection services)
{
    // AutoMapper — registra todos los perfiles del ensamblado BLL
    services.AddAutoMapper(typeof(VittalMappingProfile).Assembly);

    // FluentValidation — registra todos los validadores del ensamblado BLL
    services.AddValidatorsFromAssemblyContaining<PacienteRequestValidator>();

    // Services — Scoped (una instancia por request HTTP)
    services.AddScoped<IClinicaService,          ClinicaService>();
    services.AddScoped<IPerfilService,           PerfilService>();
    services.AddScoped<IUsuarioService,          UsuarioService>();
    services.AddScoped<IPermisoService,          PermisoService>();
    services.AddScoped<ISalaService,             SalaService>();
    services.AddScoped<IPacienteService,         PacienteService>();
    services.AddScoped<IMedicamentoService,      MedicamentoService>();
    services.AddScoped<ITipoCirugiaService,      TipoCirugiaService>();
    services.AddScoped<ICirugiaService,          CirugiaService>();
    services.AddScoped<ITipoDiagnosticoService,  TipoDiagnosticoService>();
    services.AddScoped<IDiagnosticoService,      DiagnosticoService>();
    services.AddScoped<ITratamientoService,      TratamientoService>();
    services.AddScoped<IRecomendacionService,    RecomendacionService>();
    services.AddScoped<IExamenService,           ExamenService>();
    services.AddScoped<ICitaService,             CitaService>();
    services.AddScoped<IExpedienteService,       ExpedienteService>();
    services.AddScoped<IAlertaEsperaService,     AlertaEsperaService>();

    return services;
}
```

---

## 12. Checklist de Calidad — @EspecialistaUI (BLL)

Antes de notificar al @PM que el BLL está listo:

### ServiceResult

- [ ] Toda operación retorna `ServiceResult<T>` — **nunca** lanza excepciones al Controller
- [ ] `GetAllAsync` retorna `ServiceResult<IEnumerable<TResponseDto>>`
- [ ] `GetByIdAsync` retorna `NotFound` si el registro no existe o es de otro tenant
- [ ] `CreateAsync` retorna `Created` con el DTO del registro persistido
- [ ] `UpdateAsync` verifica existencia antes de actualizar
- [ ] `DeactivateAsync` verifica existencia y retorna `Ok(true)` — **nunca** elimina

### Validaciones

- [ ] Validador FluentValidation creado para el DTO de Request
- [ ] Todos los campos obligatorios de la HU tienen regla `NotEmpty`
- [ ] Longitudes máximas definidas con `MaximumLength`
- [ ] Formatos especiales (email, teléfono, sexo) validados con `Matches` o `EmailAddress`
- [ ] Validaciones condicionales usan `.When(x => ...)` correctamente
- [ ] Los mensajes de error están en **español** y son descriptivos para el usuario final

### Reglas de negocio

- [ ] `clinicaId` siempre viene del parámetro del método — **nunca** del DTO
- [ ] Verificación de unicidad de campos únicos antes de crear/actualizar
- [ ] Verificación de existencia de entidades relacionadas (FKs) cuando aplique
- [ ] Lógica de estados correcta (Cita: agendada → en_espera → en_atencion → atendida)
- [ ] **No existe** lógica de eliminación en ningún Service

### AutoMapper

- [ ] Mapeo `Entity → ResponseDto` definido en `VittalMappingProfile`
- [ ] Mapeo `RequestDto → Entity` ignora `Id`, `ClinicaId`, `Activo`, `FechaCreacion`, `FechaModificacion`
- [ ] Campos calculados (`NombreCompleto`) mapeados con `MapFrom`

### Registro IOC

- [ ] Service e interfaz registrados en `Vittal.IOC/DependencyInjection.cs`
- [ ] Lifetime es `Scoped`
- [ ] Validador registrado via `AddValidatorsFromAssemblyContaining`
- [ ] AutoMapper registrado via `AddAutoMapper` apuntando al ensamblado BLL

---

*skill-bll.md — Vittal v1.0.0 | Agente: @EspecialistaUI*
*Para contexto del proyecto: CLAUDE.md | Para acceso a datos: skill-dal.md*
*Para coordinación de agentes: ORCHESTRATOR.md | Siguiente: skill-controller.md*
