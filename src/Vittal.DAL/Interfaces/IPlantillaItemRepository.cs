using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repository para items individuales de plantillas de especialidad.
/// Tabla global: plantilla_items — sin clinica_id.
/// </summary>
public interface IPlantillaItemRepository
{
    Task<IEnumerable<PlantillaItem>> GetByPlantillaIdAsync(Guid plantillaId);
    Task<PlantillaItem?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(PlantillaItem entity);
    Task<bool> UpdateAsync(PlantillaItem entity);
    Task<bool> DeactivateAsync(Guid id);
    Task<bool> ReactivateAsync(Guid id);
}
