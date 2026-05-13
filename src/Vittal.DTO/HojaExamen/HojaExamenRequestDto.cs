using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.HojaExamen;

/// <summary>
/// Request DTO para agregar un examen a una hoja de cita.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaExamenRequestDto
{
    /// <summary>Hoja de cita a la que pertenece el examen (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una hoja de cita.")]
    public Guid HojaCitaId { get; set; }

    /// <summary>Examen a registrar (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un examen.")]
    public Guid ExamenId { get; set; }

    /// <summary>Resultado del examen médico.</summary>
    [StringLength(2000, ErrorMessage = "El resultado no puede exceder 2000 caracteres.")]
    public string? Resultado { get; set; }

    /// <summary>URL del archivo asociado al resultado del examen.</summary>
    [StringLength(500, ErrorMessage = "La URL del archivo no puede exceder 500 caracteres.")]
    public string? ArchivoUrl { get; set; }
}
