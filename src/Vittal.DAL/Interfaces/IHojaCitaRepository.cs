using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de hojas de cita médica.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaCitaRepository
{
    /// <summary>Obtiene todas las hojas de cita activas de una clínica.</summary>
    Task<IEnumerable<HojaCita>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene una hoja de cita por ID dentro de una clínica.</summary>
    Task<HojaCita?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todas las hojas de cita activas de un expediente.</summary>
    Task<IEnumerable<HojaCita>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId);

    /// <summary>Crea una nueva hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaCita entity);

    /// <summary>Actualiza una hoja de cita existente.</summary>
    Task<bool> UpdateAsync(HojaCita entity);

    /// <summary>Desactiva una hoja de cita (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
