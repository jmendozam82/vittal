using Vittal.DTO;
using Vittal.DTO.Catalogos;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

public interface ITipoSignoVitalService
{
    Task<ServiceResult<IEnumerable<TipoSignoVitalDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid salaId);
    Task<ServiceResult<TipoSignoVitalDTOs.Response>> GetByIdAsync(Guid clinicaId, Guid id);
    Task<ServiceResult<Guid>> CreateAsync(Guid clinicaId, Guid usuarioId, TipoSignoVitalDTOs.Request request);
    Task<ServiceResult<bool>> UpdateAsync(Guid clinicaId, Guid id, TipoSignoVitalDTOs.Request request);
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
