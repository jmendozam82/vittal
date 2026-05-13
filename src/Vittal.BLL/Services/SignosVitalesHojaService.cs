using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.SignosVitalesHoja;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de lógica de negocio para signos vitales por consulta (hoja de cita).
/// NOTA: El campo FueraDeRango se calcula mediante un trigger en la BD,
///       no se calcula en este service.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class SignosVitalesHojaService : ISignosVitalesHojaService
{
    private readonly ISignosVitalesHojaRepository _repository;

    public SignosVitalesHojaService(ISignosVitalesHojaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IEnumerable<SignosVitalesHojaResponseDto>>> GetAllAsync(Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            var entities = await _repository.GetAllAsync(clinicaId, hojaCitaId);
            var dtos = entities.Select(MapToDto);
            return ServiceResult<IEnumerable<SignosVitalesHojaResponseDto>>.Success(dtos);
        }
        catch (Exception)
        {
            return ServiceResult<IEnumerable<SignosVitalesHojaResponseDto>>.Failure("Error al obtener signos vitales.");
        }
    }

    public async Task<ServiceResult<SignosVitalesHojaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<SignosVitalesHojaResponseDto>.Failure("Signo vital no encontrado.", ServiceErrorType.NotFound);
            }

            var dto = MapToDto(entity);
            return ServiceResult<SignosVitalesHojaResponseDto>.Success(dto);
        }
        catch (Exception)
        {
            return ServiceResult<SignosVitalesHojaResponseDto>.Failure("Error al obtener el signo vital.");
        }
    }

    public async Task<ServiceResult<SignosVitalesHojaResponseDto>> CreateAsync(SignosVitalesHojaRequestDto dto, Guid clinicaId, Guid usuarioId)
    {
        try
        {
            var entity = new SignosVitalesHoja
            {
                ClinicaId = clinicaId,
                HojaCitaId = dto.HojaCitaId,
                SalaId = dto.SalaId,
                TipoSignoVitalId = dto.TipoSignoVitalId,
                Valor = dto.Valor,
                Unidad = dto.Unidad,
                FechaHora = dto.FechaHora ?? DateTime.UtcNow,
                RegistradoPor = usuarioId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var id = await _repository.CreateAsync(entity);

            // Recuperar la entidad creada para obtener los nombres de JOIN y el FueraDeRango
            var created = await _repository.GetByIdAsync(clinicaId, id);
            if (created == null)
            {
                return ServiceResult<SignosVitalesHojaResponseDto>.Failure("Error al recuperar el signo vital registrado.", ServiceErrorType.InternalError);
            }

            var responseDto = MapToDto(created);
            return ServiceResult<SignosVitalesHojaResponseDto>.Success(responseDto, "Signo vital registrado exitosamente.");
        }
        catch (Exception)
        {
            return ServiceResult<SignosVitalesHojaResponseDto>.Failure("Error al registrar el signo vital.");
        }
    }

    public async Task<ServiceResult<SignosVitalesHojaResponseDto>> UpdateAsync(Guid id, SignosVitalesHojaRequestDto dto, Guid clinicaId)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<SignosVitalesHojaResponseDto>.Failure("Signo vital no encontrado.", ServiceErrorType.NotFound);
            }

            entity.HojaCitaId = dto.HojaCitaId;
            entity.SalaId = dto.SalaId;
            entity.TipoSignoVitalId = dto.TipoSignoVitalId;
            entity.Valor = dto.Valor;
            entity.Unidad = dto.Unidad;
            entity.FechaHora = dto.FechaHora ?? DateTime.UtcNow;
            entity.FechaModificacion = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);
            if (!result)
            {
                return ServiceResult<SignosVitalesHojaResponseDto>.Failure("No se pudo actualizar el signo vital.");
            }

            var updated = await _repository.GetByIdAsync(clinicaId, id);
            var responseDto = MapToDto(updated ?? entity);
            return ServiceResult<SignosVitalesHojaResponseDto>.Success(responseDto, "Signo vital actualizado exitosamente.");
        }
        catch (Exception)
        {
            return ServiceResult<SignosVitalesHojaResponseDto>.Failure("Error al actualizar el signo vital.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Signo vital desactivado exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró el signo vital.", ServiceErrorType.NotFound);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("Error al desactivar el signo vital.");
        }
    }

    // ── Mapeo manual Entity → DTO ──────────────────────────────────────

    private static SignosVitalesHojaResponseDto MapToDto(SignosVitalesHoja entity)
    {
        return new SignosVitalesHojaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            HojaCitaId = entity.HojaCitaId,
            SalaId = entity.SalaId,
            SalaNombre = entity.Sala?.Nombre ?? string.Empty,
            TipoSignoVitalId = entity.TipoSignoVitalId,
            TipoSignoVitalNombre = entity.TipoSignoVital?.Nombre ?? string.Empty,
            Valor = entity.Valor,
            Unidad = entity.Unidad,
            FueraDeRango = entity.FueraDeRango,
            FechaHora = entity.FechaHora,
            RegistradoPor = entity.RegistradoPor,
            RegistradoPorNombre = entity.Registrador?.NombreCompleto ?? string.Empty,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion
        };
    }
}
