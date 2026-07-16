using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de cirugías en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaCirugiaRepository
{
    /// <summary>Obtiene una cirugía de hoja de cita por ID dentro de una clínica.</summary>
    Task<HojaCirugia?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todas las cirugías activas de una hoja de cita.</summary>
    Task<IEnumerable<HojaCirugia>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea una nueva cirugía en la hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaCirugia entity);

    /// <summary>Actualiza una cirugía existente.</summary>
    Task<bool> UpdateAsync(HojaCirugia entity);

    /// <summary>Desactiva una cirugía (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
