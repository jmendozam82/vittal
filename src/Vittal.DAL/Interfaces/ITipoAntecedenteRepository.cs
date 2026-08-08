using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

public interface ITipoAntecedenteRepository
{
    /// <summary>Lista tipos de antecedente. Si salaId es null o Guid.Empty, devuelve todos los de la clínica.</summary>
    Task<IEnumerable<TipoAntecedente>> GetAllAsync(Guid clinicaId, Guid? salaId);
    Task<TipoAntecedente?> GetByIdAsync(Guid clinicaId, Guid id);
    Task<TipoAntecedente?> GetBySalaAndNameAsync(Guid clinicaId, Guid salaId, string nombre);
    Task<Guid> CreateAsync(TipoAntecedente entity);
    Task<bool> UpdateAsync(TipoAntecedente entity);
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
    Task<bool> ReactivateAsync(Guid clinicaId, Guid id);
}
