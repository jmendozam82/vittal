using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de citas médicas.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public interface ICitaRepository
{
    /// <summary>Obtiene todas las citas activas de una clínica.</summary>
    Task<IEnumerable<Cita>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene una cita por ID dentro de una clínica.</summary>
    Task<Cita?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Crea una nueva cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(Cita entity);

    /// <summary>Actualiza una cita existente.</summary>
    Task<bool> UpdateAsync(Cita entity);

    /// <summary>Desactiva una cita (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
