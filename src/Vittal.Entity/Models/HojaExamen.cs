namespace Vittal.Entity.Models;

/// <summary>
/// Examen registrado en una hoja de cita médica.
/// Tabla: public.hojas_examenes
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaExamen
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid ExamenId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Resultado del examen médico.</summary>
    public string? Resultado { get; set; }

    /// <summary>URL del archivo asociado (resultado digital, imagen, etc.).</summary>
    public string? ArchivoUrl { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre del examen (JOIN con examenes).</summary>
    public string ExamenNombre { get; set; } = string.Empty;
}
