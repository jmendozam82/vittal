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

    // ── Sprint 7: Reportes y Dashboard ─────────────────────────────

    /// <summary>Obtiene citas en un rango de fechas, opcionalmente filtradas por doctor y sala.</summary>
    Task<IEnumerable<Cita>> GetByDateRangeAsync(Guid clinicaId, DateTime fechaInicio, DateTime fechaFin, Guid? doctorId = null, Guid? salaId = null);

    /// <summary>Obtiene estadísticas de citas agrupadas por estado en un rango de fechas.</summary>
    Task<IEnumerable<Cita>> GetEstadisticasPorEstadoAsync(Guid clinicaId, DateTime fechaInicio, DateTime fechaFin);

    /// <summary>Obtiene los doctores más activos por cantidad de citas en un rango de fechas.</summary>
    Task<IEnumerable<Cita>> GetDoctoresMasActivosAsync(Guid clinicaId, DateTime fechaInicio, DateTime fechaFin, int limit = 10);
}
