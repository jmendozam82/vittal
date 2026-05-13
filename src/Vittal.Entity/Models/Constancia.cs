namespace Vittal.Entity.Models;

/// <summary>
/// Constancias médicas emitidas para pacientes.
/// Incluye: constancias de asistencia, incapacidad médica, referencias, etc.
/// Tabla: public.constancias
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public class Constancia
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    /// <summary>Expediente del paciente al que pertenece esta constancia.</summary>
    public Guid ExpedienteId { get; set; }

    /// <summary>Hoja de cita asociada (opcional — puede ser una constancia independiente).</summary>
    public Guid? HojaCitaId { get; set; }

    /// <summary>Doctor que emite la constancia.</summary>
    public Guid DoctorId { get; set; }

    // ── Datos de la constancia ───────────────────────────────────────
    /// <summary>Tipo de constancia: "ASISTENCIA", "INCAPACIDAD", "REFERENCIA", "JUSTIFICANTE", etc.</summary>
    public string TipoConstancia { get; set; } = string.Empty;

    /// <summary>Contenido/texto completo de la constancia (HTML o texto plano).</summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que se emitió la constancia.</summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>Días de reposo/incapacidad (solo para incapacidades médicas).</summary>
    public int? DiasReposo { get; set; }

    /// <summary>Nombre del especialista al que se refiere al paciente (solo para referencias).</summary>
    public string? EspecialistaReferido { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    /// <summary>true = vigente, false = anulada (constancias no se eliminan).</summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    /// <summary>Usuario que creó la constancia (normalmente el doctor).</summary>
    public Guid? CreadoPor { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    /// <summary>Nombre completo del doctor que emitió la constancia.</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del paciente (desde expediente/paciente).</summary>
    public string PacienteNombre { get; set; } = string.Empty;
}
