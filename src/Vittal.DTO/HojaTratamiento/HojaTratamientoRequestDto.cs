using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.HojaTratamiento;

/// <summary>
/// Request DTO para agregar un tratamiento o medicamento a una hoja de cita.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaTratamientoRequestDto
{
    /// <summary>Hoja de cita a la que pertenece el tratamiento (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una hoja de cita.")]
    public Guid HojaCitaId { get; set; }

    /// <summary>Medicamento recetado (opcional — puede ser solo tratamiento no farmacológico).</summary>
    public Guid? MedicamentoId { get; set; }

    /// <summary>Tratamiento indicado (opcional — puede ser solo medicamento).</summary>
    public Guid? TratamientoId { get; set; }

    /// <summary>Dosis del medicamento recetado.</summary>
    [StringLength(100, ErrorMessage = "La dosis no puede exceder 100 caracteres.")]
    public string? Dosis { get; set; }

    /// <summary>Frecuencia de administración.</summary>
    [StringLength(100, ErrorMessage = "La frecuencia no puede exceder 100 caracteres.")]
    public string? Frecuencia { get; set; }

    /// <summary>Duración del tratamiento.</summary>
    [StringLength(100, ErrorMessage = "La duración no puede exceder 100 caracteres.")]
    public string? Duracion { get; set; }

    /// <summary>Instrucciones adicionales para el tratamiento.</summary>
    [StringLength(1000, ErrorMessage = "Las instrucciones no pueden exceder 1000 caracteres.")]
    public string? Instrucciones { get; set; }
}
