using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para notificaciones del sistema.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface INotificacionRepository
{
    /// <summary>Obtiene notificaciones de una clínica, opcionalmente filtradas por estado de lectura.</summary>
    Task<IEnumerable<Notificacion>> GetByClinicaIdAsync(Guid clinicaId, bool? leida = null, int? limit = null);

    /// <summary>Crea una nueva notificación y retorna su ID.</summary>
    Task<Guid> CreateAsync(Notificacion entity);

    /// <summary>Marca una notificación específica como leída.</summary>
    Task<bool> MarcarLeidaAsync(Guid clinicaId, Guid id);

    /// <summary>Marca todas las notificaciones de una clínica como leídas.</summary>
    Task<bool> MarcarTodasLeidasAsync(Guid clinicaId);

    /// <summary>Obtiene la cantidad de notificaciones no leídas de una clínica.</summary>
    Task<int> GetNoLeidasCountAsync(Guid clinicaId);
}
