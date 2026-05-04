# BLL — Service Templates

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para implementar interfaces y servicios de negocio.
> **Prerequisito:** skills/bll/SKILL.md, skills/bll/service-result.md

---

## Interfaz de Service

```csharp
// src/Vittal.BLL/Interfaces/I[Entidad]Service.cs
namespace Vittal.BLL.Interfaces;

public interface I[Entidad]Service
{
    Task<ServiceResult<IEnumerable<[Entidad]ResponseDto>>> GetAllAsync(Guid clinicaId);
    Task<ServiceResult<[Entidad]ResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<[Entidad]ResponseDto>> CreateAsync([Entidad]RequestDto dto, Guid clinicaId);
    Task<ServiceResult<[Entidad]ResponseDto>> UpdateAsync(Guid id, [Entidad]RequestDto dto, Guid clinicaId);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
}
```

---

## Implementación de Service (Plantilla Maestra)

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
        // if (await _repository.ExistsAsync(clinicaId, "email", dto.Email))
        //     return ServiceResult<...>.Duplicate("Ya existe un registro con ese email.");

        try
        {
            // 3. Mapear DTO → Entity
            var entidad = _mapper.Map<[Entidad]>(dto);
            entidad.ClinicaId = clinicaId;  // Del JWT, nunca del DTO

            // 4. Persistir
            var id = await _repository.CreateAsync(entidad);

            // 5. Retornar registro creado
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
        var existente = await _repository.GetByIdAsync(id, clinicaId);
        if (existente is null)
            return ServiceResult<[Entidad]ResponseDto>.NotFound();

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<[Entidad]ResponseDto>.ValidationError(
                validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            _mapper.Map(dto, existente);
            existente.Id = id;
            existente.ClinicaId = clinicaId;

            await _repository.UpdateAsync(existente);

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
    // REGLA: Desactivar, nunca eliminar
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
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

## Checklist de Calidad — Service Templates

- [ ] Constructor recibe Repository, IMapper, IValidator, ILogger
- [ ] `GetAllAsync` con try/catch + logging
- [ ] `GetByIdAsync` retorna NotFound si entidad es null
- [ ] `CreateAsync`: valida → reglas de negocio → mapea → persiste → retorna creado
- [ ] `CreateAsync`: clinicaId asignado desde parámetro, nunca desde DTO
- [ ] `UpdateAsync`: verifica existencia antes de actualizar
- [ ] `UpdateAsync`: preserva Id y ClinicaId tras mapeo
- [ ] `DeactivateAsync`: verifica existencia → DeactivateAsync del repo
- [ **No existe método DeleteAsync en ningún Service
- [ ] Todos los catch logean con `_logger.LogError`
- [ ] Todos los catch retornan `ServerError()`

---

*skills/bll/service-templates.md — Vittal v1.0.0*
