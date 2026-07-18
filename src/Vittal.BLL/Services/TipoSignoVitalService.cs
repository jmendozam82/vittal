using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.Utility.Results;
using Vittal.DTO;
using Vittal.DTO.Catalogos;
using Vittal.Entity;

namespace Vittal.BLL.Services;

public class TipoSignoVitalService : ITipoSignoVitalService
{
    private readonly ITipoSignoVitalRepository _repository;
    private readonly ILogger<TipoSignoVitalService> _logger;

    public TipoSignoVitalService(ITipoSignoVitalRepository repository, ILogger<TipoSignoVitalService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<TipoSignoVitalDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid salaId)
    {
        try
        {
            var entities = await _repository.GetAllAsync(clinicaId, salaId);
            var dtos = entities.Select(e => new TipoSignoVitalDTOs.Response
            {
                Id = e.Id,
                SalaId = e.SalaId,
                Nombre = e.Nombre,
                Unidad = e.Unidad,
                ValorMin = e.ValorMin,
                ValorMax = e.ValorMax,
                Orden = e.Orden,
                EsObligatorio = e.EsObligatorio
            });

            return ServiceResult<IEnumerable<TipoSignoVitalDTOs.Response>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de signos vitales para ClinicaId={ClinicaId}, SalaId={SalaId}", clinicaId, salaId);
            return ServiceResult<IEnumerable<TipoSignoVitalDTOs.Response>>.Failure("Error al obtener tipos de signos vitales.");
        }
    }

    public async Task<ServiceResult<TipoSignoVitalDTOs.Response>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<TipoSignoVitalDTOs.Response>.Failure("Tipo de signo vital no encontrado.");
            }

            var dto = new TipoSignoVitalDTOs.Response
            {
                Id = entity.Id,
                SalaId = entity.SalaId,
                Nombre = entity.Nombre,
                Unidad = entity.Unidad,
                ValorMin = entity.ValorMin,
                ValorMax = entity.ValorMax,
                Orden = entity.Orden,
                EsObligatorio = entity.EsObligatorio
            };

            return ServiceResult<TipoSignoVitalDTOs.Response>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipo de signo vital Id={Id} para ClinicaId={ClinicaId}", id, clinicaId);
            return ServiceResult<TipoSignoVitalDTOs.Response>.Failure("Error al obtener el tipo de signo vital.");
        }
    }

    public async Task<ServiceResult<Guid>> CreateAsync(Guid clinicaId, Guid usuarioId, TipoSignoVitalDTOs.Request request)
    {
        try
        {
            var entity = new TipoSignoVital
            {
                ClinicaId = clinicaId,
                SalaId = request.SalaId,
                Nombre = request.Nombre,
                Unidad = request.Unidad,
                ValorMin = request.ValorMin,
                ValorMax = request.ValorMax,
                Orden = request.Orden,
                EsObligatorio = request.EsObligatorio,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = usuarioId
            };

            var id = await _repository.CreateAsync(entity);

            return ServiceResult<Guid>.Success(id, "Tipo de signo vital creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de signo vital '{Nombre}' para ClinicaId={ClinicaId}", request.Nombre, clinicaId);
            return ServiceResult<Guid>.Failure("Error al crear el tipo de signo vital.");
        }
    }

    public async Task<ServiceResult<bool>> UpdateAsync(Guid clinicaId, Guid id, TipoSignoVitalDTOs.Request request)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<bool>.Failure("Tipo de signo vital no encontrado.");
            }

            entity.Nombre = request.Nombre;
            entity.Unidad = request.Unidad;
            entity.ValorMin = request.ValorMin;
            entity.ValorMax = request.ValorMax;
            entity.Orden = request.Orden;
            entity.EsObligatorio = request.EsObligatorio;
            entity.FechaModificacion = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);

            return result
                ? ServiceResult<bool>.Success(result, "Actualizado exitosamente.")
                : ServiceResult<bool>.Failure("No se pudo actualizar.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de signo vital Id={Id} para ClinicaId={ClinicaId}", id, clinicaId);
            return ServiceResult<bool>.Failure("Error al actualizar.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Desactivado exitosamente.")
                : ServiceResult<bool>.Failure("No se encontrÃ³ el registro.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tipo de signo vital Id={Id} para ClinicaId={ClinicaId}", id, clinicaId);
            return ServiceResult<bool>.Failure("Error al desactivar.");
        }
    }
}
