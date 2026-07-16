using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para la configuración de alertas de tiempo de espera por clínica.
/// Es una relación 1:1 con clinica_id — solo existe un registro por clínica.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface IConfiguracionAlertaRepository
{
    /// <summary>Obtiene la configuración de alertas de una clínica.</summary>
    Task<ConfiguracionAlerta?> GetByClinicaIdAsync(Guid clinicaId);

    /// <summary>Crea o actualiza la configuración de alertas de una clínica (upsert).</summary>
    Task<Guid> CreateOrUpdateAsync(ConfiguracionAlerta entity);
}
