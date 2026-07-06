using Vittal.DTO;
using Vittal.DTO.Plantillas;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

public interface IPlantillaEspecialidadService
{
    Task<ServiceResult<IEnumerable<PlantillaEspecialidadDTOs.Response>>> GetAllAsync();
    Task<ServiceResult<PlantillaEspecialidadDTOs.Response>> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(PlantillaEspecialidadDTOs.Request request);
    Task<ServiceResult<bool>> UpdateAsync(Guid id, PlantillaEspecialidadDTOs.Request request);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id);
}
