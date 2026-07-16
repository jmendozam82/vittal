namespace Vittal.Entity;

/// <summary>
/// Notificaciones del sistema para usuarios de la clínica (alertas, avisos, etc.).
/// Tabla: public.notificaciones
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class Notificacion
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    /// <summary>ID de la alerta relacionada (opcional, para alertas de tiempo de espera).</summary>
    public Guid? AlertaId { get; set; }

    // ── Campos de contenido ───────────────────────────────────────
    /// <summary>Tipo de notificación: alerta_espera | informacion | advertencia | exito.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Título corto de la notificación.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Mensaje detallado de la notificación.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Nombre del icono a mostrar (opcional, ej: "clock", "bell").</summary>
    public string? Icono { get; set; }

    /// <summary>Color de la notificación (opcional, ej: "warning", "danger", "success").</summary>
    public string? Color { get; set; }

    // ── Campos de estado ──────────────────────────────────────────
    /// <summary>Indica si la notificación ha sido leída por el usuario destino.</summary>
    public bool Leida { get; set; }

    /// <summary>Usuario destino de la notificación (null = todos los usuarios de la clínica).</summary>
    public Guid? UsuarioDestinoId { get; set; }

    /// <summary>Fecha y hora en que se leyó la notificación.</summary>
    public DateTime? FechaLectura { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
