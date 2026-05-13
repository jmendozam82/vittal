using Vittal.DTO.AntecedentesPaciente;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

public interface IAntecedentePacienteService
{
    Task<ServiceResult<IEnumerable<AntecedentePacienteDTOs.Response>>> GetAllAsync(Guid clinicaId, Guid expedienteId, Guid salaId);
    Task<ServiceResult<AntecedentePacienteDTOs.Response>> GetByIdAsync(Guid clinicaId, Guid id);
    Task<ServiceResult<AntecedentePacienteDTOs.Response>> UpsertAsync(AntecedentePacienteDTOs.Request request, Guid clinicaId, Guid expedienteId, Guid usuarioId);
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
