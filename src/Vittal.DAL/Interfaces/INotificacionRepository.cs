using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para notificaciones del sistema.
/// El estado de lectura es individual por usuario (tabla notificaciones_usuario).
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface INotificacionRepository : IPaginatedRepository<Notificacion>
{
    /// <summary>Obtiene notificaciones de un usuario (vía asignación), opcionalmente filtrado por estado de lectura.</summary>
    Task<IEnumerable<Notificacion>> GetByClinicaIdAsync(Guid clinicaId, Guid usuarioId, bool? leida = null, int? limit = null);

    /// <summary>Crea una nueva notificación y la asigna a los usuarios destino
    /// (uno específico si UsuarioDestinoId tiene valor; a todos los activos de la clínica si es null).</summary>
    Task<Guid> CreateAsync(Notificacion entity);

    /// <summary>Marca una notificación como leída para un usuario específico.</summary>
    Task<bool> MarcarLeidaAsync(Guid clinicaId, Guid usuarioId, Guid notificacionId);

    /// <summary>Marca todas las notificaciones de un usuario específico como leídas.</summary>
    Task<bool> MarcarTodasLeidasAsync(Guid clinicaId, Guid usuarioId);

    /// <summary>Obtiene la cantidad de notificaciones no leídas de un usuario específico.</summary>
    Task<int> GetNoLeidasCountAsync(Guid clinicaId, Guid usuarioId);
}