using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.Utility.Results;
using Vittal.DTO.Plantillas;
using Vittal.Entity.Models;

namespace Vittal.BLL.Services;

public class PlantillaItemService : IPlantillaItemService
{
    private readonly IPlantillaItemRepository _repository;

    public PlantillaItemService(IPlantillaItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IEnumerable<PlantillaItemDTOs.Response>>> GetByPlantillaIdAsync(Guid plantillaId)
    {
        try
        {
            var entities = await _repository.GetByPlantillaIdAsync(plantillaId);
            var dtos = entities.Select(MapToResponse);
            return ServiceResult<IEnumerable<PlantillaItemDTOs.Response>>.Success(dtos);
        }
        catch (Exception)
        {
            return ServiceResult<IEnumerable<PlantillaItemDTOs.Response>>.Failure("Error al obtener los items de la plantilla.");
        }
    }

    public async Task<ServiceResult<PlantillaItemDTOs.Response>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<PlantillaItemDTOs.Response>.Failure("Item no encontrado.");
            }
            return ServiceResult<PlantillaItemDTOs.Response>.Success(MapToResponse(entity));
        }
        catch (Exception)
        {
            return ServiceResult<PlantillaItemDTOs.Response>.Failure("Error al obtener el item.");
        }
    }

    public async Task<ServiceResult<Guid>> CreateAsync(PlantillaItemDTOs.Request request)
    {
        try
        {
            var entity = new PlantillaItem
            {
                PlantillaId = request.PlantillaId,
                TipoItem = request.TipoItem,
                Nombre = request.Nombre,
                Categoria = request.Categoria,
                TipoDato = request.TipoDato,
                Unidad = request.Unidad,
                ValorMin = request.ValorMin,
                ValorMax = request.ValorMax,
                EsObligatorio = request.EsObligatorio,
                Orden = request.Orden,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var id = await _repository.CreateAsync(entity);
            return ServiceResult<Guid>.Success(id, "Item creado exitosamente.");
        }
        catch (Exception)
        {
            return ServiceResult<Guid>.Failure("Error al crear el item.");
        }
    }

    public async Task<ServiceResult<bool>> UpdateAsync(Guid id, PlantillaItemDTOs.Request request)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<bool>.Failure("Item no encontrado.");
            }

            entity.TipoItem = request.TipoItem;
            entity.Nombre = request.Nombre;
            entity.Categoria = request.Categoria;
            entity.TipoDato = request.TipoDato;
            entity.Unidad = request.Unidad;
            entity.ValorMin = request.ValorMin;
            entity.ValorMax = request.ValorMax;
            entity.EsObligatorio = request.EsObligatorio;
            entity.Orden = request.Orden;

            var result = await _repository.UpdateAsync(entity);
            return result
                ? ServiceResult<bool>.Success(result, "Item actualizado exitosamente.")
                : ServiceResult<bool>.Failure("No se pudo actualizar el item.");
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("Error al actualizar el item.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(id);
            return result
                ? ServiceResult<bool>.Success(result, "Item desactivado exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró el item.");
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("Error al desactivar el item.");
        }
    }

    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id)
    {
        try
        {
            var result = await _repository.ReactivateAsync(id);
            return result
                ? ServiceResult<bool>.Success(result, "Item reactivado exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró el item.");
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("Error al reactivar el item.");
        }
    }

    private static PlantillaItemDTOs.Response MapToResponse(PlantillaItem entity)
    {
        return new PlantillaItemDTOs.Response
        {
            Id = entity.Id,
            PlantillaId = entity.PlantillaId,
            TipoItem = entity.TipoItem,
            Nombre = entity.Nombre,
            Categoria = entity.Categoria,
            TipoDato = entity.TipoDato,
            Unidad = entity.Unidad,
            ValorMin = entity.ValorMin,
            ValorMax = entity.ValorMax,
            EsObligatorio = entity.EsObligatorio,
            Orden = entity.Orden
        };
    }
}
