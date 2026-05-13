using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.AntecedentesPaciente;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de lógica de negocio para antecedentes del paciente por sala/especialidad.
/// Historia de Usuario: HU-E05 — Antecedentes del Paciente
/// </summary>
public class AntecedentePacienteService : IAntecedentePacienteService
{
    private readonly IAntecedentePacienteRepository _repository;

    public AntecedentePacienteService(IAntecedentePacienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IEnumerable<AntecedentePacienteDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid expedienteId, Guid salaId)
    {
        try
        {
            var entities = await _repository.GetAllAsync(clinicaId, expedienteId, salaId);
            var dtos = entities.Select(MapToDto);
            return ServiceResult<IEnumerable<AntecedentePacienteDTOs.Response>>.Success(dtos);
        }
        catch (Exception)
        {
            return ServiceResult<IEnumerable<AntecedentePacienteDTOs.Response>>.Failure("Error al obtener antecedentes del paciente.");
        }
    }

    public async Task<ServiceResult<AntecedentePacienteDTOs.Response>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<AntecedentePacienteDTOs.Response>.Failure("Antecedente no encontrado.", ServiceErrorType.NotFound);
            }

            var dto = MapToDto(entity);
            return ServiceResult<AntecedentePacienteDTOs.Response>.Success(dto);
        }
        catch (Exception)
        {
            return ServiceResult<AntecedentePacienteDTOs.Response>.Failure("Error al obtener el antecedente.");
        }
    }

    public async Task<ServiceResult<AntecedentePacienteDTOs.Response>> UpsertAsync(
        AntecedentePacienteDTOs.Request request, Guid clinicaId, Guid expedienteId, Guid usuarioId)
    {
        try
        {
            var entity = new AntecedentePaciente
            {
                ClinicaId = clinicaId,
                ExpedienteId = expedienteId,
                SalaId = request.SalaId,
                TipoAntecedenteId = request.TipoAntecedenteId,
                Valor = request.Valor,
                FechaActualizacion = DateTime.UtcNow,
                ActualizadoPor = usuarioId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var id = await _repository.UpsertAsync(entity);

            // Recuperar el antecedente creado/actualizado para obtener los nombres de JOIN
            var created = await _repository.GetByIdAsync(clinicaId, id);
            if (created == null)
            {
                return ServiceResult<AntecedentePacienteDTOs.Response>.Failure("Error al recuperar el antecedente guardado.", ServiceErrorType.InternalError);
            }

            var responseDto = MapToDto(created);
            return ServiceResult<AntecedentePacienteDTOs.Response>.Success(responseDto, "Antecedente guardado exitosamente.");
        }
        catch (Exception)
        {
            return ServiceResult<AntecedentePacienteDTOs.Response>.Failure("Error al guardar el antecedente.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Antecedente desactivado exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró el antecedente.", ServiceErrorType.NotFound);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("Error al desactivar el antecedente.");
        }
    }

    // ── Mapeo manual Entity → DTO ──────────────────────────────────────

    private static AntecedentePacienteDTOs.Response MapToDto(AntecedentePaciente entity)
    {
        return new AntecedentePacienteDTOs.Response
        {
            Id = entity.Id,
            ExpedienteId = entity.ExpedienteId,
            SalaId = entity.SalaId,
            SalaNombre = entity.Sala?.Nombre ?? string.Empty,
            TipoAntecedenteId = entity.TipoAntecedenteId,
            TipoAntecedenteNombre = entity.TipoAntecedente?.Nombre ?? string.Empty,
            TipoAntecedenteCategoria = entity.TipoAntecedente?.Categoria,
            TipoAntecedenteTipoDato = entity.TipoAntecedente?.TipoDato ?? string.Empty,
            Valor = entity.Valor,
            FechaActualizacion = entity.FechaActualizacion,
            ActualizadoPor = entity.ActualizadoPor
        };
    }
}
