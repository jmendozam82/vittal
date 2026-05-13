using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Paciente;

/// <summary>
/// Request DTO para crear o editar un paciente.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public class PacienteRequestDto
{
    /// <summary>Doctor al que está asignado el paciente (obligatorio).</summary>
    [Required(ErrorMessage = "Debe asignar un doctor al paciente.")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer nombre debe tener entre 2 y 100 caracteres.")]
    public string PrimerNombre { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo nombre no puede exceder 100 caracteres.")]
    public string? SegundoNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer apellido debe tener entre 2 y 100 caracteres.")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no puede exceder 100 caracteres.")]
    public string? SegundoApellido { get; set; }

    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(255, ErrorMessage = "El correo no puede exceder 255 caracteres.")]
    public string? Email { get; set; }

    [StringLength(20, ErrorMessage = "El celular no puede exceder 20 caracteres.")]
    public string? Celular { get; set; }

    public string? Direccion { get; set; }

    /// <summary>M = Masculino, F = Femenino</summary>
    public string? Sexo { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    /// <summary>URL de la foto del paciente (Supabase Storage).</summary>
    public string? FotoUrl { get; set; }

    public string? Observaciones { get; set; }
}
