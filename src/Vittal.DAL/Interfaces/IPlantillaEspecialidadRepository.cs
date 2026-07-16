using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repository for global PlantillaEspecialidad entities.
/// Does NOT use clinicaId since it is a global catalog.
/// </summary>
public interface IPlantillaEspecialidadRepository
{
    Task<IEnumerable<PlantillaEspecialidad>> GetAllAsync();
    Task<PlantillaEspecialidad?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(PlantillaEspecialidad entity);
    Task<bool> UpdateAsync(PlantillaEspecialidad entity);
    Task<bool> DeactivateAsync(Guid id);
    Task<bool> ReactivateAsync(Guid id);
}
