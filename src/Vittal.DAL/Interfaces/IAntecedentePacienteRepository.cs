using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

public interface IAntecedentePacienteRepository
{
    Task<IEnumerable<AntecedentePaciente>> GetAllAsync(Guid clinicaId, Guid expedienteId, Guid salaId);
    Task<AntecedentePaciente?> GetByIdAsync(Guid clinicaId, Guid id);
    Task<Guid> UpsertAsync(AntecedentePaciente entity);
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
