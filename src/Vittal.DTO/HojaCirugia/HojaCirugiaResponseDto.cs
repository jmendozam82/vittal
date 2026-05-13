using System;

namespace Vittal.DTO.HojaCirugia;

/// <summary>
/// Response DTO para una cirugía registrada en una hoja de cita.
/// Incluye nombres de la cirugía y tipo mediante JOINs.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCirugiaResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid CirugiaId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public DateTime? FechaCirugia { get; set; }
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre de la cirugía (JOIN con cirugias).</summary>
    public string CirugiaNombre { get; set; } = string.Empty;

    /// <summary>Nombre del tipo de cirugía (JOIN con tipos_cirugia).</summary>
    public string TipoCirugiaNombre { get; set; } = string.Empty;
}
