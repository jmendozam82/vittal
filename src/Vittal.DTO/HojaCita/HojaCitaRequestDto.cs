using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.HojaCita;

/// <summary>
/// Request DTO para crear o editar una hoja de cita médica.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCitaRequestDto
{
    /// <summary>Expediente al que pertenece esta hoja de cita (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un expediente.")]
    public Guid ExpedienteId { get; set; }

    /// <summary>Cita médica asociada (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar una cita.")]
    public Guid CitaId { get; set; }

    /// <summary>Doctor que realizó la consulta (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un doctor.")]
    public Guid DoctorId { get; set; }

    /// <summary>Fecha en que se realizó la consulta.</summary>
    public DateTime? FechaConsulta { get; set; }

    /// <summary>Motivo de la consulta expresado por el paciente.</summary>
    [StringLength(500, ErrorMessage = "El motivo de consulta no puede exceder 500 caracteres.")]
    public string? MotivoConsulta { get; set; }

    /// <summary>Notas clínicas del doctor durante la consulta.</summary>
    [StringLength(2000, ErrorMessage = "Las notas de consulta no pueden exceder 2000 caracteres.")]
    public string? NotasConsulta { get; set; }
}
