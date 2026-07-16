namespace Vittal.Entity;

/// <summary>
/// Expediente médico de un paciente. Contiene el historial completo de hojas de cita,
/// diagnósticos, tratamientos, cirugías, exámenes y archivos asociados.
/// Tabla: public.expedientes
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class Expediente
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid DoctorId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Notas generales del expediente médico.</summary>
    public string? NotasGenerales { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre completo del paciente (JOIN con pacientes).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del doctor (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;
}
