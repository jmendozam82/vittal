using System;

namespace Vittal.DTO.Cita;

/// <summary>
/// Response DTO para datos de una cita médica.
/// Incluye nombres del paciente, doctor y sala mediante JOINs.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public class CitaResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? SalaId { get; set; }

    // ── Campos de fecha y hora ────────────────────────────────────
    public DateTime FechaCita { get; set; }
    public TimeSpan HoraCita { get; set; }
    public TimeSpan? HoraFin { get; set; }
    public TimeSpan? HoraLlegada { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public string? Lugar { get; set; }
    public string? Motivo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Notas { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre completo del paciente (JOIN con pacientes).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del doctor (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre de la sala (JOIN con salas).</summary>
    public string? SalaNombre { get; set; }
}
