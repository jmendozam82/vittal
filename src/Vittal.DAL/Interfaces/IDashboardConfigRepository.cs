using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para la configuración del dashboard por clínica.
/// Es una relación 1:1 con clinica_id — solo existe un registro por clínica.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public interface IDashboardConfigRepository
{
    /// <summary>Obtiene la configuración del dashboard de una clínica.</summary>
    Task<DashboardConfig?> GetByClinicaIdAsync(Guid clinicaId);

    /// <summary>Crea o actualiza la configuración del dashboard de una clínica (upsert).</summary>
    Task<Guid> CreateOrUpdateAsync(DashboardConfig entity);
}
