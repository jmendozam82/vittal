namespace Vittal.Entity;

/// <summary>
/// Cirugía registrada en una hoja de cita médica.
/// Tabla: public.hoja_cirugias
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCirugia
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid CirugiaId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Fecha programada o realizada de la cirugía.</summary>
    public DateTime? FechaCirugia { get; set; }

    /// <summary>Observaciones adicionales sobre la cirugía.</summary>
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre de la cirugía (JOIN con cirugias).</summary>
    public string CirugiaNombre { get; set; } = string.Empty;

    /// <summary>Nombre del tipo de cirugía (JOIN con tipos_cirugia).</summary>
    public string TipoCirugiaNombre { get; set; } = string.Empty;
}
