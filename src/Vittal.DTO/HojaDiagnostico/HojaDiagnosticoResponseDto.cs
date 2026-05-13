using System;

namespace Vittal.DTO.HojaDiagnostico;

/// <summary>
/// Response DTO para un diagnóstico registrado en una hoja de cita.
/// Incluye nombres del diagnóstico y tipo mediante JOINs.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaDiagnosticoResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid DiagnosticoId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre del diagnóstico (JOIN con diagnosticos).</summary>
    public string DiagnosticoNombre { get; set; } = string.Empty;

    /// <summary>Nombre del tipo de diagnóstico (JOIN con tipos_diagnostico).</summary>
    public string TipoDiagnosticoNombre { get; set; } = string.Empty;
}
