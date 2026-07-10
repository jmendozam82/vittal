using System;

namespace Vittal.DTO.HojaRecomendacion;

/// <summary>
/// Response DTO para una recomendación registrada en una hoja de cita.
/// Incluye el nombre de la recomendación mediante JOIN.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaRecomendacionResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid RecomendacionId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre de la recomendación (JOIN con recomendaciones).</summary>
    public string RecomendacionNombre { get; set; } = string.Empty;
}
