using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de exámenes en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaExamenRepository
{
    /// <summary>Obtiene un examen de hoja de cita por ID dentro de una clínica.</summary>
    Task<HojaExamen?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los exámenes activos de una hoja de cita.</summary>
    Task<IEnumerable<HojaExamen>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo examen en la hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaExamen entity);

    /// <summary>Actualiza un examen existente.</summary>
    Task<bool> UpdateAsync(HojaExamen entity);

    /// <summary>Desactiva un examen (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
