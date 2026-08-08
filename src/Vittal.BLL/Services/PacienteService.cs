using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Paciente;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de pacientes. Implementa IPacienteService.
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public class PacienteService : IPacienteService
{
    private readonly IPacienteRepository _repo;
    private readonly ILogger<PacienteService> _logger;
    private readonly IValidator<PacienteRequestDto> _validator;

    public PacienteService(
        IPacienteRepository repo,
        ILogger<PacienteService> logger,
        IValidator<PacienteRequestDto> validator)
    {
        _repo = repo;
        _logger = logger;
        _validator = validator;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista pacientes de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<PacienteResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo pacientes de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<PacienteResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapPacienteToDto(entity));
            }

            return ServiceResult<IEnumerable<PacienteResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un paciente por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<PacienteResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando paciente {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "Paciente no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<PacienteResponseDto>.Success(MapPacienteToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar paciente {Id}", id);
            return ServiceResult<PacienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo paciente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<PacienteResponseDto>> CreateAsync(
        PacienteRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando paciente {PrimerNombre} {PrimerApellido} en clínica {ClinicaId}",
                dto.PrimerNombre, dto.PrimerApellido, clinicaId);

            // Validación FluentValidation — reemplaza la validación inline eliminada
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<PacienteResponseDto>.Failure(
                    string.Join("; ", errors), ServiceErrorType.Validation, errors);
            }

            // Validate numero documento uniqueness
            if (await _repo.ExistsByNumeroDocumentoAsync(clinicaId, dto.NumeroDocumentoIdentificacion, null))
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "Ya existe un paciente con ese número de documento de identificación en esta clínica",
                    ServiceErrorType.Conflict);
            }

            // Validate uniqueness
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (await _repo.ExistsByEmailAsync(clinicaId, dto.Email))
                {
                    return ServiceResult<PacienteResponseDto>.Failure(
                        "Ya existe un paciente con ese correo electrónico en esta clínica.",
                        ServiceErrorType.Conflict);
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Celular))
            {
                if (await _repo.ExistsByCelularAsync(clinicaId, dto.Celular))
                {
                    return ServiceResult<PacienteResponseDto>.Failure(
                        "Ya existe un paciente con ese número de celular en esta clínica.",
                        ServiceErrorType.Conflict);
                }
            }

            var entity = new Paciente
            {
                ClinicaId = clinicaId,
                DoctorId = dto.DoctorId,
                PrimerNombre = dto.PrimerNombre,
                SegundoNombre = dto.SegundoNombre,
                PrimerApellido = dto.PrimerApellido,
                SegundoApellido = dto.SegundoApellido,
                Email = dto.Email,
                Celular = dto.Celular,
                Direccion = dto.Direccion,
                Sexo = dto.Sexo,
                FechaNacimiento = dto.FechaNacimiento,
                FotoUrl = dto.FotoUrl,
                Observaciones = dto.Observaciones,
                TipoDocumentoIdentificacion = dto.TipoDocumentoIdentificacion,
                NumeroDocumentoIdentificacion = dto.NumeroDocumentoIdentificacion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Paciente creado con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "Paciente creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<PacienteResponseDto>.Success(
                MapPacienteToDto(created), "Paciente creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear paciente en clínica {ClinicaId}", clinicaId);
            return ServiceResult<PacienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del paciente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<PacienteResponseDto>> UpdateAsync(
        Guid id, PacienteRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando paciente {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "Paciente no encontrado", ServiceErrorType.NotFound);
            }

            // Validación FluentValidation — reemplaza la validación inline eliminada
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<PacienteResponseDto>.Failure(
                    string.Join("; ", errors), ServiceErrorType.Validation, errors);
            }

            // Validate numero documento uniqueness (excluding current patient)
            if (await _repo.ExistsByNumeroDocumentoAsync(clinicaId, dto.NumeroDocumentoIdentificacion, id))
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "Ya existe un paciente con ese número de documento de identificación en esta clínica",
                    ServiceErrorType.Conflict);
            }

            // Validate uniqueness (exclude current)
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (await _repo.ExistsByEmailAsync(clinicaId, dto.Email, id))
                {
                    return ServiceResult<PacienteResponseDto>.Failure(
                        "Ya existe otro paciente con ese correo electrónico en esta clínica.",
                        ServiceErrorType.Conflict);
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Celular))
            {
                if (await _repo.ExistsByCelularAsync(clinicaId, dto.Celular, id))
                {
                    return ServiceResult<PacienteResponseDto>.Failure(
                        "Ya existe otro paciente con ese número de celular en esta clínica.",
                        ServiceErrorType.Conflict);
                }
            }

            // Update entity fields
            existing.DoctorId = dto.DoctorId;
            existing.PrimerNombre = dto.PrimerNombre;
            existing.SegundoNombre = dto.SegundoNombre;
            existing.PrimerApellido = dto.PrimerApellido;
            existing.SegundoApellido = dto.SegundoApellido;
            existing.Email = dto.Email;
            existing.Celular = dto.Celular;
            existing.Direccion = dto.Direccion;
            existing.Sexo = dto.Sexo;
            existing.FechaNacimiento = dto.FechaNacimiento;
            existing.FotoUrl = dto.FotoUrl;
            existing.Observaciones = dto.Observaciones;
            existing.TipoDocumentoIdentificacion = dto.TipoDocumentoIdentificacion;
            existing.NumeroDocumentoIdentificacion = dto.NumeroDocumentoIdentificacion;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "No se pudo actualizar el paciente.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<PacienteResponseDto>.Failure(
                    "Paciente actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<PacienteResponseDto>.Success(
                MapPacienteToDto(refreshed), "Paciente actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar paciente {Id}", id);
            return ServiceResult<PacienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva paciente (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando paciente {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Paciente no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El paciente ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el paciente.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Paciente desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar paciente {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ReactivateAsync — Reactiva paciente (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando paciente {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Paciente no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El paciente ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el paciente.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Paciente reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar paciente {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de pacientes por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<PacienteResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando pacientes con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<PacienteResponseDto>>.Success(
                    new List<PacienteResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            var entities = await _repo.SearchAsync(clinicaId, term.Trim());
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Success(
                entities.Select(e => MapPacienteToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar pacientes en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. GetByDoctorAsync — Lista pacientes activos asignados a un doctor
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<PacienteResponseDto>>> GetByDoctorAsync(
        Guid clinicaId, Guid doctorId)
    {
        try
        {
            _logger.LogInformation("Obteniendo pacientes del doctor {DoctorId} en clínica {ClinicaId}",
                doctorId, clinicaId);

            var entities = await _repo.GetAllAsync(clinicaId);

            var dtos = entities
                .Where(p => p.DoctorId == doctorId)
                .Select(MapPacienteToDto)
                .ToList();

            return ServiceResult<IEnumerable<PacienteResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes del doctor {DoctorId} en clínica {ClinicaId}",
                doctorId, clinicaId);
            return ServiceResult<IEnumerable<PacienteResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static PacienteResponseDto MapPacienteToDto(Paciente p)
    {
        return new PacienteResponseDto
        {
            Id = p.Id,
            ClinicaId = p.ClinicaId,
            DoctorId = p.DoctorId,
            DoctorNombre = p.DoctorNombre,
            PrimerNombre = p.PrimerNombre,
            SegundoNombre = p.SegundoNombre,
            PrimerApellido = p.PrimerApellido,
            SegundoApellido = p.SegundoApellido,
            Email = p.Email,
            Celular = p.Celular,
            Direccion = p.Direccion,
            Sexo = p.Sexo,
            TipoDocumentoIdentificacion = p.TipoDocumentoIdentificacion,
            NumeroDocumentoIdentificacion = p.NumeroDocumentoIdentificacion,
            FechaNacimiento = p.FechaNacimiento,
            FotoUrl = p.FotoUrl,
            Observaciones = p.Observaciones,
            Activo = p.Activo,
            FechaCreacion = p.FechaCreacion,
            FechaModificacion = p.FechaModificacion,
            NombreCompleto = p.NombreCompleto
        };
    }
}
