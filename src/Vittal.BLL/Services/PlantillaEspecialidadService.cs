using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.Utility.Results;
using Vittal.DTO;
using Vittal.DTO.Plantillas;
using Vittal.Entity;

namespace Vittal.BLL.Services;

public class PlantillaEspecialidadService : IPlantillaEspecialidadService
{
    private readonly IPlantillaEspecialidadRepository _repository;
    private readonly ILogger<PlantillaEspecialidadService> _logger;

    public PlantillaEspecialidadService(IPlantillaEspecialidadRepository repository, ILogger<PlantillaEspecialidadService> logger)
    {
        _repository = repository;
        _logger = logger;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<PlantillaEspecialidadDTOs.Response>>> GetAllAsync()
    {
        try
        {
            var entities = await _repository.GetAllAsync();
            var dtos = entities.Select(e => new PlantillaEspecialidadDTOs.Response
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                Icono = e.Icono,
                Items = e.Items.Select(i => new PlantillaItemDTOs.Response
                {
                    Id = i.Id,
                    PlantillaId = i.PlantillaId,
                    TipoItem = i.TipoItem,
                    Nombre = i.Nombre,
                    Categoria = i.Categoria,
                    TipoDato = i.TipoDato,
                    Unidad = i.Unidad,
                    ValorMin = i.ValorMin,
                    ValorMax = i.ValorMax,
                    EsObligatorio = i.EsObligatorio,
                    Orden = i.Orden
                })
            });

            return ServiceResult<IEnumerable<PlantillaEspecialidadDTOs.Response>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las plantillas de especialidad");
            return ServiceResult<IEnumerable<PlantillaEspecialidadDTOs.Response>>.Failure("Error al obtener plantillas de especialidad.");
        }
    }

    public async Task<ServiceResult<PlantillaEspecialidadDTOs.Response>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<PlantillaEspecialidadDTOs.Response>.Failure("Plantilla no encontrada.");
            }

            var dto = new PlantillaEspecialidadDTOs.Response
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Icono = entity.Icono,
                Items = entity.Items.Select(i => new PlantillaItemDTOs.Response
                {
                    Id = i.Id,
                    PlantillaId = i.PlantillaId,
                    TipoItem = i.TipoItem,
                    Nombre = i.Nombre,
                    Categoria = i.Categoria,
                    TipoDato = i.TipoDato,
                    Unidad = i.Unidad,
                    ValorMin = i.ValorMin,
                    ValorMax = i.ValorMax,
                    EsObligatorio = i.EsObligatorio,
                    Orden = i.Orden
                })
            };

            return ServiceResult<PlantillaEspecialidadDTOs.Response>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener plantilla de especialidad Id={Id}", id);
            return ServiceResult<PlantillaEspecialidadDTOs.Response>.Failure("Error al obtener la plantilla.");
        }
    }

    public async Task<ServiceResult<Guid>> CreateAsync(PlantillaEspecialidadDTOs.Request request)
    {
        try
        {
            var entity = new PlantillaEspecialidad
            {
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                Icono = request.Icono,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                Items = request.Items?.Select(i => new PlantillaItem
                {
                    PlantillaId = Guid.Empty, // se asigna en el repository
                    TipoItem = i.TipoItem,
                    Nombre = i.Nombre,
                    Categoria = i.Categoria,
                    TipoDato = i.TipoDato,
                    Unidad = i.Unidad,
                    ValorMin = i.ValorMin,
                    ValorMax = i.ValorMax,
                    EsObligatorio = i.EsObligatorio,
                    Orden = i.Orden,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }).ToList() ?? new List<PlantillaItem>()
            };

            var id = await _repository.CreateAsync(entity);
            return ServiceResult<Guid>.Success(id, "Plantilla creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear plantilla de especialidad '{Nombre}'", request.Nombre);
            return ServiceResult<Guid>.Failure("Error al crear la plantilla.");
        }
    }

    public async Task<ServiceResult<bool>> UpdateAsync(Guid id, PlantillaEspecialidadDTOs.Request request)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<bool>.Failure("Plantilla no encontrada.");
            }

            entity.Nombre = request.Nombre;
            entity.Descripcion = request.Descripcion;
            entity.Icono = request.Icono;
            entity.FechaModificacion = DateTime.UtcNow;

            // Nota: Los items se gestionan por separado via PlantillaItemController.
            // Este endpoint solo actualiza el header de la plantilla.

            var result = await _repository.UpdateAsync(entity);
            return result
                ? ServiceResult<bool>.Success(result, "Plantilla actualizada exitosamente.")
                : ServiceResult<bool>.Failure("No se pudo actualizar la plantilla.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar plantilla de especialidad Id={Id}", id);
            return ServiceResult<bool>.Failure("Error al actualizar la plantilla.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(id);
            return result
                ? ServiceResult<bool>.Success(result, "Plantilla desactivada exitosamente.")
                : ServiceResult<bool>.Failure("No se encontrÃƒÂ³ la plantilla.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar plantilla de especialidad Id={Id}", id);
            return ServiceResult<bool>.Failure("Error al desactivar la plantilla.");
        }
    }

    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id)
    {
        try
        {
            var result = await _repository.ReactivateAsync(id);
            return result
                ? ServiceResult<bool>.Success(result, "Plantilla reactivada exitosamente.")
                : ServiceResult<bool>.Failure("No se encontrÃƒÂ³ la plantilla.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar plantilla de especialidad Id={Id}", id);
            return ServiceResult<bool>.Failure("Error al reactivar la plantilla.");
        }
    }
}
