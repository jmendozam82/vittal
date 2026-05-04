using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Repositories;
using Vittal.DTO.Perfil;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Implementación de lógica de negocio para Perfil.
/// Historia de Usuario: HU03 — Gestión de Perfiles
/// </summary>
public class PerfilService : IPerfilService
{
    private readonly IPerfilRepository _perfilRepository;
    private readonly ILogger<PerfilService> _logger;

    public PerfilService(IPerfilRepository perfilRepository, ILogger<PerfilService> logger)
    {
        _perfilRepository = perfilRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<PerfilResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            var perfiles = await _perfilRepository.GetAllAsync(clinicaId);
            var dtos = MapToResponseDto(perfiles);
            return ServiceResult<IEnumerable<PerfilResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfiles de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<PerfilResponseDto>>.Failure("Error interno al consultar perfiles.");
        }
    }

    public async Task<ServiceResult<PerfilResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            var perfil = await _perfilRepository.GetByIdAsync(id, clinicaId);
            if (perfil == null)
                return ServiceResult<PerfilResponseDto>.Failure(
                    "Perfil no encontrado en esta clínica.", ServiceErrorType.NotFound);

            return ServiceResult<PerfilResponseDto>.Success(MapToResponseDto(perfil));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfil {Id}", id);
            return ServiceResult<PerfilResponseDto>.Failure("Error interno al consultar el perfil.");
        }
    }

    public async Task<ServiceResult<PerfilResponseDto>> CreateAsync(PerfilRequestDto dto, Guid clinicaId)
    {
        // Validación manual
        var validationErrors = ValidateDto(dto);
        if (validationErrors.Count > 0)
            return ServiceResult<PerfilResponseDto>.Failure(
                string.Join("; ", validationErrors), ServiceErrorType.Validation, validationErrors);

        // Regla de negocio: nombre único por clínica
        var nombreExiste = await _perfilRepository.ExistsByNameAsync(clinicaId, dto.Nombre);
        if (nombreExiste)
            return ServiceResult<PerfilResponseDto>.Failure(
                $"Ya existe un perfil con el nombre '{dto.Nombre}' en esta clínica.",
                ServiceErrorType.Conflict);

        try
        {
            var perfil = MapToEntity(dto);
            perfil.ClinicaId = clinicaId;

            var id = await _perfilRepository.CreateAsync(perfil);

            // Retornar el perfil recién creado
            var created = await _perfilRepository.GetByIdAsync(id, clinicaId);
            return ServiceResult<PerfilResponseDto>.Success(
                MapToResponseDto(created!), "Perfil creado exitosamente.");
        }
        catch (DuplicateEntityException ex)
        {
            return ServiceResult<PerfilResponseDto>.Failure(ex.Message, ServiceErrorType.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear perfil en clínica {ClinicaId}", clinicaId);
            return ServiceResult<PerfilResponseDto>.Failure("Error interno al crear el perfil.");
        }
    }

    public async Task<ServiceResult<PerfilResponseDto>> UpdateAsync(Guid id, PerfilRequestDto dto, Guid clinicaId)
    {
        var existente = await _perfilRepository.GetByIdAsync(id, clinicaId);
        if (existente == null)
            return ServiceResult<PerfilResponseDto>.Failure(
                "Perfil no encontrado en esta clínica.", ServiceErrorType.NotFound);

        var validationErrors = ValidateDto(dto);
        if (validationErrors.Count > 0)
            return ServiceResult<PerfilResponseDto>.Failure(
                string.Join("; ", validationErrors), ServiceErrorType.Validation, validationErrors);

        // Verificar unicidad excluyendo el registro actual
        var nombreExiste = await _perfilRepository.ExistsByNameAsync(clinicaId, dto.Nombre, id);
        if (nombreExiste)
            return ServiceResult<PerfilResponseDto>.Failure(
                $"Ya existe un perfil con el nombre '{dto.Nombre}' en esta clínica.",
                ServiceErrorType.Conflict);

        try
        {
            existente.Nombre = dto.Nombre;
            existente.Descripcion = dto.Descripcion;
            existente.EsAdmin = dto.EsAdmin;

            await _perfilRepository.UpdateAsync(existente);

            var updated = await _perfilRepository.GetByIdAsync(id, clinicaId);
            return ServiceResult<PerfilResponseDto>.Success(
                MapToResponseDto(updated!), "Perfil actualizado exitosamente.");
        }
        catch (DuplicateEntityException ex)
        {
            return ServiceResult<PerfilResponseDto>.Failure(ex.Message, ServiceErrorType.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil {Id}", id);
            return ServiceResult<PerfilResponseDto>.Failure("Error interno al actualizar el perfil.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        var existente = await _perfilRepository.GetByIdAsync(id, clinicaId);
        if (existente == null)
            return ServiceResult<bool>.Failure(
                "Perfil no encontrado en esta clínica.", ServiceErrorType.NotFound);

        try
        {
            var result = await _perfilRepository.DeactivateAsync(id, clinicaId);

            if (!result)
            {
                // Verificar si tiene usuarios asignados
                var usuarioCount = await _perfilRepository.CountUsuariosAsync(id, clinicaId);
                if (usuarioCount > 0)
                    return ServiceResult<bool>.Failure(
                        $"No se puede desactivar el perfil '{existente.Nombre}' porque tiene {usuarioCount} usuario(s) asignado(s). Primero reasigne los usuarios a otro perfil.",
                        ServiceErrorType.Conflict);

                return ServiceResult<bool>.Failure("No fue posible desactivar el perfil.");
            }

            return ServiceResult<bool>.Success(true, "Perfil desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar perfil {Id}", id);
            return ServiceResult<bool>.Failure("Error interno al desactivar el perfil.");
        }
    }

    // ── Mapeo manual (sin AutoMapper) ──────────────────────────────────────

    private static Perfil MapToEntity(PerfilRequestDto dto)
    {
        return new Perfil
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            EsAdmin = dto.EsAdmin
        };
    }

    private static PerfilResponseDto MapToResponseDto(Perfil perfil)
    {
        return new PerfilResponseDto
        {
            Id = perfil.Id,
            Nombre = perfil.Nombre,
            Descripcion = perfil.Descripcion,
            EsAdmin = perfil.EsAdmin,
            Activo = perfil.Activo,
            FechaCreacion = perfil.FechaCreacion,
            FechaModificacion = perfil.FechaModificacion,
            CantidadPermisos = perfil.CantidadPermisos,
            CantidadUsuarios = perfil.CantidadUsuarios
        };
    }

    private static IEnumerable<PerfilResponseDto> MapToResponseDto(IEnumerable<Perfil> perfiles)
    {
        foreach (var perfil in perfiles)
            yield return MapToResponseDto(perfil);
    }

    // ── Validación manual ──────────────────────────────────────────────────

    private static List<string> ValidateDto(PerfilRequestDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            errors.Add("El nombre del perfil es obligatorio.");
        else if (dto.Nombre.Length < 3)
            errors.Add("El nombre debe tener al menos 3 caracteres.");
        else if (dto.Nombre.Length > 100)
            errors.Add("El nombre no puede exceder 100 caracteres.");

        if (dto.Descripcion != null && dto.Descripcion.Length > 500)
            errors.Add("La descripción no puede exceder 500 caracteres.");

        return errors;
    }
}
