namespace Vittal.Entity;

/// <summary>
/// Estado de lectura individual por notificación y usuario.
/// Modelo estándar: la notificación (mensaje) es compartida por clínica y
/// cada usuario tiene su propio marcador de leído en esta tabla hija.
/// Tabla: public.notificaciones_usuario
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class NotificacionUsuario
{
    public Guid Id { get; set; }

    /// <summary>Notificación (mensaje) a la que pertenece el estado de lectura.</summary>
    public Guid NotificacionId { get; set; }

    /// <summary>Usuario destinatario. El marcado de leído es individual.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Indica si el usuario ha leído la notificación.</summary>
    public bool Leida { get; set; }

    /// <summary>Fecha y hora en que el usuario marcó la notificación como leída.</summary>
    public DateTime? FechaLectura { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
