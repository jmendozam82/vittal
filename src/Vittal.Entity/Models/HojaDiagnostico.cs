namespace Vittal.Entity.Models;

/// <summary>
/// Diagnóstico registrado en una hoja de cita médica.
/// Tabla: public.hoja_diagnosticos
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaDiagnostico
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid DiagnosticoId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Observaciones adicionales del diagnóstico.</summary>
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre del diagnóstico (JOIN con diagnosticos).</summary>
    public string DiagnosticoNombre { get; set; } = string.Empty;

    /// <summary>Nombre del tipo de diagnóstico (JOIN con tipos_diagnostico).</summary>
    public string TipoDiagnosticoNombre { get; set; } = string.Empty;
}
