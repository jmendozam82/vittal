using System;

namespace Vittal.DTO.HojaTratamiento;

/// <summary>
/// Response DTO para un tratamiento registrado en una hoja de cita.
/// Incluye nombres del medicamento y tratamiento mediante JOINs.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaTratamientoResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid? MedicamentoId { get; set; }
    public Guid? TratamientoId { get; set; }

    // ── Campos de prescripción ────────────────────────────────────
    public string? Dosis { get; set; }
    public string? Frecuencia { get; set; }
    public string? Duracion { get; set; }
    public string? Instrucciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre del medicamento (JOIN con medicamentos).</summary>
    public string? MedicamentoNombre { get; set; }

    /// <summary>Nombre del tratamiento (JOIN con tratamientos).</summary>
    public string? TratamientoNombre { get; set; }
}
