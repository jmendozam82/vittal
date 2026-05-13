using System;

namespace Vittal.DTO.Diagnostico;

/// <summary>
/// Response DTO para datos del diagnóstico asignado a una cita.
/// Incluye nombre del tipo de diagnóstico mediante JOIN con tipos_diagnostico.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public class DiagnosticoResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid CitaId { get; set; }
    public Guid TipoDiagnosticoId { get; set; }
    public string TipoDiagnosticoNombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
