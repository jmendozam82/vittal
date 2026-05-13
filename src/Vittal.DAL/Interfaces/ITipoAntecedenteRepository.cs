using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

public interface ITipoAntecedenteRepository
{
    Task<IEnumerable<TipoAntecedente>> GetAllAsync(Guid clinicaId, Guid salaId);
    Task<TipoAntecedente?> GetByIdAsync(Guid clinicaId, Guid id);
    Task<Guid> CreateAsync(TipoAntecedente entity);
    Task<bool> UpdateAsync(TipoAntecedente entity);
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
