namespace Vittal.Entity;

/// <summary>
/// Hoja de cita médica que vincula una cita (Cita) con los datos clínicos
/// registrados durante la consulta (signos vitales, diagnósticos, tratamientos, etc.).
/// Tabla: public.hojas_cita
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCita
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid CitaId { get; set; }
    public Guid ExpedienteId { get; set; }
    public Guid DoctorId { get; set; }

    // ── Campos de la consulta ─────────────────────────────────────
    /// <summary>Fecha en que se realizó la consulta médica.</summary>
    public DateTime FechaConsulta { get; set; }

    /// <summary>Motivo de la consulta expresado por el paciente.</summary>
    public string? MotivoConsulta { get; set; }

    /// <summary>Notas clínicas del doctor durante la consulta.</summary>
    public string? NotasConsulta { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre completo del paciente (JOIN con pacientes vía expediente).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del doctor (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>
    /// Estado de la cita asociada (JOIN con citas).
    /// 'atendida' indica que la consulta fue finalizada y no debe editarse.
    /// </summary>
    public string? CitaEstado { get; set; }
}
