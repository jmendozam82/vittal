using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de tratamientos y medicamentos en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaTratamientoRepository
{
    /// <summary>Obtiene un tratamiento de hoja de cita por ID dentro de una clínica.</summary>
    Task<HojaTratamiento?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los tratamientos activos de una hoja de cita.</summary>
    Task<IEnumerable<HojaTratamiento>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo tratamiento en la hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaTratamiento entity);

    /// <summary>Actualiza un tratamiento existente.</summary>
    Task<bool> UpdateAsync(HojaTratamiento entity);

    /// <summary>Desactiva un tratamiento (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
