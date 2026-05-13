using Vittal.DTO;
using Vittal.DTO.Catalogos;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

public interface ITipoAntecedenteService
{
    Task<ServiceResult<IEnumerable<TipoAntecedenteDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid salaId);
    Task<ServiceResult<TipoAntecedenteDTOs.Response>> GetByIdAsync(Guid clinicaId, Guid id);
    Task<ServiceResult<Guid>> CreateAsync(Guid clinicaId, Guid usuarioId, TipoAntecedenteDTOs.Request request);
    Task<ServiceResult<bool>> UpdateAsync(Guid clinicaId, Guid id, TipoAntecedenteDTOs.Request request);
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
