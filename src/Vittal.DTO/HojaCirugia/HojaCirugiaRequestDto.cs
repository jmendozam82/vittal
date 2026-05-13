using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.HojaCirugia;

/// <summary>
/// Request DTO para agregar una cirugía a una hoja de cita.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCirugiaRequestDto
{
    /// <summary>Hoja de cita a la que pertenece la cirugía (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una hoja de cita.")]
    public Guid HojaCitaId { get; set; }

    /// <summary>Cirugía a registrar (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una cirugía.")]
    public Guid CirugiaId { get; set; }

    /// <summary>Fecha programada o realizada de la cirugía.</summary>
    public DateTime? FechaCirugia { get; set; }

    /// <summary>Observaciones adicionales sobre la cirugía.</summary>
    [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
    public string? Observaciones { get; set; }
}
