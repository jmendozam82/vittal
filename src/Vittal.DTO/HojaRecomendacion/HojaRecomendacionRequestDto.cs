using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.HojaRecomendacion;

/// <summary>
/// Request DTO para agregar una recomendación a una hoja de cita.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaRecomendacionRequestDto
{
    /// <summary>Hoja de cita a la que pertenece la recomendación (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una hoja de cita.")]
    public Guid HojaCitaId { get; set; }

    /// <summary>Recomendación a registrar (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una recomendación.")]
    public Guid RecomendacionId { get; set; }

    /// <summary>Observaciones adicionales sobre la recomendación.</summary>
    [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
    public string? Observaciones { get; set; }
}
