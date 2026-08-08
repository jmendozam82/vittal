using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.Utility.Results;
using Vittal.DTO;
using Vittal.DTO.Catalogos;
using Vittal.Entity;

namespace Vittal.BLL.Services;

public class TipoAntecedenteService : ITipoAntecedenteService
{
    private readonly ITipoAntecedenteRepository _repository;
    private readonly ILogger<TipoAntecedenteService> _logger;

    public TipoAntecedenteService(
        ITipoAntecedenteRepository repository,
        ILogger<TipoAntecedenteService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<ServiceResult<IEnumerable<TipoAntecedenteDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid salaId)
        => GetAllAsync(clinicaId, (Guid?)salaId);

    /// <summary>
    /// Lista tipos de antecedente. Si salaId es null o Guid.Empty, devuelve todos los de la clínica.
    /// </summary>
    public async Task<ServiceResult<IEnumerable<TipoAntecedenteDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid? salaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo tipos de antecedentes para clínica {ClinicaId} sala {SalaId}", clinicaId, salaId);
            var entities = await _repository.GetAllAsync(clinicaId, salaId);
            var dtos = entities.Select(e => new TipoAntecedenteDTOs.Response
            {
                Id = e.Id,
                SalaId = e.SalaId,
                Nombre = e.Nombre,
                Categoria = e.Categoria,
                TipoDato = e.TipoDato,
                Orden = e.Orden
            });

            return ServiceResult<IEnumerable<TipoAntecedenteDTOs.Response>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de antecedentes para clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TipoAntecedenteDTOs.Response>>.Failure("Error al obtener tipos de antecedentes.");
        }
    }

    public async Task<ServiceResult<TipoAntecedenteDTOs.Response>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<TipoAntecedenteDTOs.Response>.Failure("Tipo de antecedente no encontrado.");
            }

            var dto = new TipoAntecedenteDTOs.Response
            {
                Id = entity.Id,
                SalaId = entity.SalaId,
                Nombre = entity.Nombre,
                Categoria = entity.Categoria,
                TipoDato = entity.TipoDato,
                Orden = entity.Orden
            };

            return ServiceResult<TipoAntecedenteDTOs.Response>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipo de antecedente Id={Id} para ClinicaId={ClinicaId}", id, clinicaId);
            return ServiceResult<TipoAntecedenteDTOs.Response>.Failure("Error al obtener el tipo de antecedente.");
        }
    }

    public async Task<ServiceResult<Guid>> CreateAsync(Guid clinicaId, Guid usuarioId, TipoAntecedenteDTOs.Request request)
    {
        try
        {
            var entity = new TipoAntecedente
            {
                ClinicaId = clinicaId,
                SalaId = request.SalaId,
                Nombre = request.Nombre,
                Categoria = request.Categoria,
                TipoDato = request.TipoDato,
                Orden = request.Orden,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = usuarioId
            };

            var id = await _repository.CreateAsync(entity);

            return ServiceResult<Guid>.Success(id, "Tipo de antecedente creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de antecedente '{Nombre}' para ClinicaId={ClinicaId}", request.Nombre, clinicaId);
            return ServiceResult<Guid>.Failure("Error al crear el tipo de antecedente.");
        }
    }

    public async Task<ServiceResult<bool>> UpdateAsync(Guid clinicaId, Guid id, TipoAntecedenteDTOs.Request request)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<bool>.Failure("Tipo de antecedente no encontrado.");
            }

            entity.Nombre = request.Nombre;
            entity.Categoria = request.Categoria;
            entity.TipoDato = request.TipoDato;
            entity.Orden = request.Orden;
            entity.FechaModificacion = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);

            return result
                ? ServiceResult<bool>.Success(result, "Actualizado exitosamente.")
                : ServiceResult<bool>.Failure("No se pudo actualizar.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de antecedente Id={Id} para ClinicaId={ClinicaId}", id, clinicaId);
            return ServiceResult<bool>.Failure("Error al actualizar el tipo de antecedente.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Desactivado exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró el registro.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tipo de antecedente Id={Id} para ClinicaId={ClinicaId}", id, clinicaId);
            return ServiceResult<bool>.Failure("Error al desactivar.");
        }
    }
}
