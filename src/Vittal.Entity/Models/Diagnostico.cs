namespace Vittal.Entity;

/// <summary>
/// Catálogo de diagnósticos por clínica, clasificados por tipo de diagnóstico.
/// Tabla: public.diagnosticos
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public class Diagnostico
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Datos del diagnóstico ─────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoCie10 { get; set; }
    public Guid TipoDiagnosticoId { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    /// <summary>Nombre del tipo de diagnóstico (JOIN con tipos_diagnostico).</summary>
    public string TipoDiagnosticoNombre { get; set; } = string.Empty;
}
