using System;

namespace Vittal.DTO.HojaCita;

/// <summary>
/// Response DTO para datos de una hoja de cita médica.
/// Incluye nombres del paciente y doctor mediante JOINs.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCitaResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid ExpedienteId { get; set; }
    public Guid CitaId { get; set; }
    public Guid DoctorId { get; set; }

    // ── Campos de la consulta ─────────────────────────────────────
    public DateTime FechaConsulta { get; set; }
    public string? MotivoConsulta { get; set; }
    public string? NotasConsulta { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
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
