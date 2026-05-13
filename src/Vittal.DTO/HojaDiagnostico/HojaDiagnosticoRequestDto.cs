using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.HojaDiagnostico;

/// <summary>
/// Request DTO para agregar un diagnóstico a una hoja de cita.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaDiagnosticoRequestDto
{
    /// <summary>Hoja de cita a la que pertenece el diagnóstico (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una hoja de cita.")]
    public Guid HojaCitaId { get; set; }

    /// <summary>Diagnóstico a registrar (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un diagnóstico.")]
    public Guid DiagnosticoId { get; set; }

    /// <summary>Observaciones adicionales del diagnóstico.</summary>
    [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
    public string? Observaciones { get; set; }
}
