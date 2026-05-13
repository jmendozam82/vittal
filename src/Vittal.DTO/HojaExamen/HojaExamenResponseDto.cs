using System;

namespace Vittal.DTO.HojaExamen;

/// <summary>
/// Response DTO para un examen registrado en una hoja de cita.
/// Incluye el nombre del examen mediante JOIN.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaExamenResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid ExamenId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public string? Resultado { get; set; }
    public string? ArchivoUrl { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre del examen (JOIN con examenes).</summary>
    public string ExamenNombre { get; set; } = string.Empty;
}
