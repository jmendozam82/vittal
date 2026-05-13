using System;

namespace Vittal.DTO.Expediente;

/// <summary>
/// Response DTO para datos de un expediente médico.
/// Incluye nombres del paciente y doctor mediante JOINs.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid DoctorId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public string? NotasGenerales { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre completo del paciente (JOIN con pacientes).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del doctor (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;
}
