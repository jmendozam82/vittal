using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

public interface ITipoSignoVitalRepository
{
    Task<IEnumerable<TipoSignoVital>> GetAllAsync(Guid clinicaId, Guid salaId);
    Task<TipoSignoVital?> GetByIdAsync(Guid clinicaId, Guid id);
    Task<Guid> CreateAsync(TipoSignoVital entity);
    Task<bool> UpdateAsync(TipoSignoVital entity);
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
