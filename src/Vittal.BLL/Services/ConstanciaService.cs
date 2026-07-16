using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Constancia;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de lÃ³gica de negocio para constancias mÃ©dicas.
/// Las constancias son documentos legales: NO se pueden editar despuÃ©s de emitidas.
/// Solo se pueden crear y anular (no eliminar).
/// Historia de Usuario: HU-E07 â€” Constancias MÃ©dicas
/// </summary>
public class ConstanciaService : IConstanciaService
{
    private readonly IConstanciaRepository _repository;
    private readonly ILogger<ConstanciaService> _logger;

    public ConstanciaService(IConstanciaRepository repository, ILogger<ConstanciaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<ConstanciaResponseDto>>> GetAllAsync(Guid clinicaId, Guid? expedienteId = null)
    {
        try
        {
            var entities = await _repository.GetAllAsync(clinicaId, expedienteId);
            var dtos = entities.Select(MapToDto);
            return ServiceResult<IEnumerable<ConstanciaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<ConstanciaResponseDto>>.Failure("Error al obtener las constancias.");
        }
    }

    public async Task<ServiceResult<ConstanciaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<ConstanciaResponseDto>.Failure("Constancia no encontrada.", ServiceErrorType.NotFound);
            }

            var dto = MapToDto(entity);
            return ServiceResult<ConstanciaResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return ServiceResult<ConstanciaResponseDto>.Failure("Error al obtener la constancia.");
        }
    }

    public async Task<ServiceResult<ConstanciaResponseDto>> CreateAsync(ConstanciaRequestDto dto, Guid clinicaId, Guid usuarioId)
    {
        try
        {
            var entity = new Constancia
            {
                ClinicaId = clinicaId,
                ExpedienteId = dto.ExpedienteId,
                HojaCitaId = dto.HojaCitaId,
                DoctorId = dto.DoctorId,
                TipoConstancia = dto.TipoConstancia,
                Contenido = dto.Contenido,
                FechaEmision = dto.FechaEmision ?? DateTime.UtcNow,
                DiasReposo = dto.DiasReposo,
                EspecialistaReferido = dto.EspecialistaReferido,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = usuarioId
            };

            var id = await _repository.CreateAsync(entity);

            // Recuperar la constancia creada para obtener los nombres de JOIN
            var created = await _repository.GetByIdAsync(clinicaId, id);
            if (created == null)
            {
                return ServiceResult<ConstanciaResponseDto>.Failure("Error al recuperar la constancia emitida.", ServiceErrorType.InternalError);
            }

            var responseDto = MapToDto(created);
            return ServiceResult<ConstanciaResponseDto>.Success(responseDto, "Constancia emitida exitosamente.");
        }
        catch (Exception ex)
        {
            return ServiceResult<ConstanciaResponseDto>.Failure("Error al emitir la constancia.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Constancia anulada exitosamente.")
                : ServiceResult<bool>.Failure("No se encontrÃ³ la constancia.", ServiceErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure("Error al anular la constancia.");
        }
    }

    // â”€â”€ Mapeo manual Entity â†’ DTO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static ConstanciaResponseDto MapToDto(Constancia entity)
    {
        return new ConstanciaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            ExpedienteId = entity.ExpedienteId,
            HojaCitaId = entity.HojaCitaId,
            DoctorId = entity.DoctorId,
            DoctorNombre = entity.DoctorNombre,
            PacienteNombre = entity.PacienteNombre,
            TipoConstancia = entity.TipoConstancia,
            Contenido = entity.Contenido,
            FechaEmision = entity.FechaEmision,
            DiasReposo = entity.DiasReposo,
            EspecialistaReferido = entity.EspecialistaReferido,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion
        };
    }
}
