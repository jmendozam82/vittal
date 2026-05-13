using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Expediente;

/// <summary>
/// Request DTO para crear o editar un expediente médico.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteRequestDto
{
    /// <summary>Paciente al que pertenece el expediente (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un paciente.")]
    public Guid PacienteId { get; set; }

    /// <summary>Doctor responsable del expediente (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un doctor.")]
    public Guid DoctorId { get; set; }

    /// <summary>Notas generales del expediente médico.</summary>
    [StringLength(2000, ErrorMessage = "Las notas generales no pueden exceder 2000 caracteres.")]
    public string? NotasGenerales { get; set; }
}
