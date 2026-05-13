using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.Utility.Results;
using Vittal.DTO;
using Vittal.DTO.Plantillas;
using Vittal.Entity.Models;

namespace Vittal.BLL.Services;

public class PlantillaEspecialidadService : IPlantillaEspecialidadService
{
    private readonly IPlantillaEspecialidadRepository _repository;

    public PlantillaEspecialidadService(IPlantillaEspecialidadRepository repository)
    {
        _repository = repository;
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
        catch (Exception)
        {
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
        catch (Exception)
        {
            return ServiceResult<PlantillaEspecialidadDTOs.Response>.Failure("Error al obtener la plantilla.");
        }
    }

    public Task<ServiceResult<Guid>> CreateAsync(PlantillaEspecialidadDTOs.Request request)
    {
        // NOTA: Para no sobredimensionar en esta fase, la creación de plantillas
        // y sus ítems es bastante simple aquí. El admin puede crearlas con la migración.
        return Task.FromResult(ServiceResult<Guid>.Failure("Not implemented yet for admin creation via API."));
    }

    public Task<ServiceResult<bool>> UpdateAsync(Guid id, PlantillaEspecialidadDTOs.Request request)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("Not implemented yet for admin update via API."));
    }

    public Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("Not implemented yet for admin delete via API."));
    }
}
