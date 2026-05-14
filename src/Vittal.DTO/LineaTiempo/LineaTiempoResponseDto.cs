using System;

namespace Vittal.DTO.LineaTiempo;

/// <summary>
/// Response DTO para un paso de la línea de tiempo de una cita.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public class LineaTiempoResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid CitaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? SalaId { get; set; }

    // ── Campos del paso ───────────────────────────────────────────
    public string NombrePaso { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Estado { get; set; } = string.Empty;

    // ── Campos de tiempo ──────────────────────────────────────────
    public TimeSpan? HoraLlegada { get; set; }
    public TimeSpan? HoraSalida { get; set; }

    /// <summary>Duración del paso formateada (ej: "00:15:30" o "--:--:--" si no ha finalizado).</summary>
    public string DuracionFormateada { get; set; } = string.Empty;

    // ── Campos JOIN ───────────────────────────────────────────────
    /// <summary>Nombre completo del paciente (JOIN con pacientes).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre de la sala (JOIN con salas).</summary>
    public string? SalaNombre { get; set; }
}
