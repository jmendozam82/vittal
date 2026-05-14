using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para alertas de tiempo de espera de pacientes.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface IAlertaEsperaRepository
{
    /// <summary>Obtiene todas las alertas de espera de una clínica, opcionalmente filtradas por estado resuelta.</summary>
    Task<IEnumerable<AlertaEspera>> GetAllByClinicaIdAsync(Guid clinicaId, bool? resuelta = null);

    /// <summary>Obtiene las alertas de espera no resueltas de una clínica.</summary>
    Task<IEnumerable<AlertaEspera>> GetNoResueltasAsync(Guid clinicaId);

    /// <summary>Crea una nueva alerta de espera y retorna su ID.</summary>
    Task<Guid> CreateAsync(AlertaEspera entity);

    /// <summary>Marca una alerta de espera como resuelta.</summary>
    Task<bool> MarcarResueltaAsync(Guid clinicaId, Guid id);
}
