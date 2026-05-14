using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Configuración de alertas de tiempo de espera por clínica.
/// Tabla: public.configuracion_alertas
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class ConfiguracionAlerta
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Tiempo máximo de espera en minutos antes de disparar una alerta.</summary>
    public int TiempoEsperaMaximoMinutos { get; set; }

    /// <summary>Indica si las alertas están habilitadas (true) o deshabilitadas (false) para la clínica.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Indica si la notificación debe reproducir un sonido.</summary>
    public bool NotificacionSonido { get; set; } = true;

    /// <summary>Intervalo en segundos entre revisiones de tiempos de espera.</summary>
    public int IntervaloRevisionSegundos { get; set; } = 30;

    // ── Campos de auditoría ───────────────────────────────────────
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
