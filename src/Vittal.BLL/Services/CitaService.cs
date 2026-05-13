using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Cita;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de lógica de negocio para citas médicas.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public class CitaService : ICitaService
{
    private readonly ICitaRepository _repository;

    public CitaService(ICitaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IEnumerable<CitaResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            var entities = await _repository.GetAllAsync(clinicaId);
            var dtos = entities.Select(MapToDto);
            return ServiceResult<IEnumerable<CitaResponseDto>>.Success(dtos);
        }
        catch (Exception)
        {
            return ServiceResult<IEnumerable<CitaResponseDto>>.Failure("Error al obtener las citas.");
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<CitaResponseDto>.Failure("Cita no encontrada.", ServiceErrorType.NotFound);
            }

            var dto = MapToDto(entity);
            return ServiceResult<CitaResponseDto>.Success(dto);
        }
        catch (Exception)
        {
            return ServiceResult<CitaResponseDto>.Failure("Error al obtener la cita.");
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> CreateAsync(CitaRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            var entity = new Cita
            {
                ClinicaId = clinicaId,
                PacienteId = dto.PacienteId,
                DoctorId = dto.DoctorId,
                SalaId = dto.SalaId,
                FechaCita = dto.FechaCita,
                HoraCita = dto.HoraCita,
                HoraFin = dto.HoraFin,
                HoraLlegada = dto.HoraLlegada,
                Lugar = dto.Lugar,
                Motivo = dto.Motivo,
                Estado = dto.Estado,
                Notas = dto.Notas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = creadoPor
            };

            var id = await _repository.CreateAsync(entity);

            // Recuperar la entidad creada para obtener los nombres de JOIN
            var created = await _repository.GetByIdAsync(clinicaId, id);
            if (created == null)
            {
                return ServiceResult<CitaResponseDto>.Failure("Error al recuperar la cita creada.", ServiceErrorType.InternalError);
            }

            var responseDto = MapToDto(created);
            return ServiceResult<CitaResponseDto>.Success(responseDto, "Cita creada exitosamente.");
        }
        catch (Exception)
        {
            return ServiceResult<CitaResponseDto>.Failure("Error al crear la cita.");
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> UpdateAsync(Guid id, CitaRequestDto dto, Guid clinicaId)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<CitaResponseDto>.Failure("Cita no encontrada.", ServiceErrorType.NotFound);
            }

            entity.PacienteId = dto.PacienteId;
            entity.DoctorId = dto.DoctorId;
            entity.SalaId = dto.SalaId;
            entity.FechaCita = dto.FechaCita;
            entity.HoraCita = dto.HoraCita;
            entity.HoraFin = dto.HoraFin;
            entity.HoraLlegada = dto.HoraLlegada;
            entity.Lugar = dto.Lugar;
            entity.Motivo = dto.Motivo;
            entity.Estado = dto.Estado;
            entity.Notas = dto.Notas;
            entity.FechaModificacion = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);
            if (!result)
            {
                return ServiceResult<CitaResponseDto>.Failure("No se pudo actualizar la cita.");
            }

            var updated = await _repository.GetByIdAsync(clinicaId, id);
            var responseDto = MapToDto(updated ?? entity);
            return ServiceResult<CitaResponseDto>.Success(responseDto, "Cita actualizada exitosamente.");
        }
        catch (Exception)
        {
            return ServiceResult<CitaResponseDto>.Failure("Error al actualizar la cita.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Cita desactivada exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró la cita.", ServiceErrorType.NotFound);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("Error al desactivar la cita.");
        }
    }

    // ── Mapeo manual Entity → DTO ──────────────────────────────────────

    private static CitaResponseDto MapToDto(Cita entity)
    {
        return new CitaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            PacienteId = entity.PacienteId,
            DoctorId = entity.DoctorId,
            SalaId = entity.SalaId,
            FechaCita = entity.FechaCita,
            HoraCita = entity.HoraCita,
            HoraFin = entity.HoraFin,
            HoraLlegada = entity.HoraLlegada,
            Lugar = entity.Lugar,
            Motivo = entity.Motivo,
            Estado = entity.Estado,
            Notas = entity.Notas,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion,
            PacienteNombre = entity.PacienteNombre,
            DoctorNombre = entity.DoctorNombre,
            SalaNombre = entity.SalaNombre
        };
    }
}
