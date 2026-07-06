using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

public interface ITipoSignoVitalRepository
{
    Task<IEnumerable<TipoSignoVital>> GetAllAsync(Guid clinicaId, Guid salaId);
    Task<TipoSignoVital?> GetByIdAsync(Guid clinicaId, Guid id);
    Task<TipoSignoVital?> GetBySalaAndNameAsync(Guid clinicaId, Guid salaId, string nombre);
    Task<Guid> CreateAsync(TipoSignoVital entity);
    Task<bool> UpdateAsync(TipoSignoVital entity);
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
    Task<bool> ReactivateAsync(Guid clinicaId, Guid id);
}
