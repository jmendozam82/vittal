namespace Vittal.Entity;

/// <summary>
/// Recomendación registrada en una hoja de cita médica.
/// Tabla: public.hojas_recomendaciones
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaRecomendacion
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid RecomendacionId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Observaciones adicionales sobre la recomendación.</summary>
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre de la recomendación (JOIN con recomendaciones).</summary>
    public string RecomendacionNombre { get; set; } = string.Empty;
}
