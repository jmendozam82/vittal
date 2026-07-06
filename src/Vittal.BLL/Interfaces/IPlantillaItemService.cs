using Vittal.DTO.Plantillas;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Servicio para gestión de items individuales dentro de plantillas de especialidad.
/// </summary>
public interface IPlantillaItemService
{
    Task<ServiceResult<IEnumerable<PlantillaItemDTOs.Response>>> GetByPlantillaIdAsync(Guid plantillaId);
    Task<ServiceResult<PlantillaItemDTOs.Response>> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(PlantillaItemDTOs.Request request);
    Task<ServiceResult<bool>> UpdateAsync(Guid id, PlantillaItemDTOs.Request request);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id);
}
