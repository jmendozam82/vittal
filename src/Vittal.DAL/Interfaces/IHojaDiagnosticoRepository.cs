using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de diagnósticos en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaDiagnosticoRepository
{
    /// <summary>Obtiene un diagnóstico de hoja de cita por ID dentro de una clínica.</summary>
    Task<HojaDiagnostico?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los diagnósticos activos de una hoja de cita.</summary>
    Task<IEnumerable<HojaDiagnostico>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo diagnóstico en la hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaDiagnostico entity);

    /// <summary>Actualiza un diagnóstico existente.</summary>
    Task<bool> UpdateAsync(HojaDiagnostico entity);

    /// <summary>Desactiva un diagnóstico (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
