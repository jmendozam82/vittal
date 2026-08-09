using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Cita;

/// <summary>
/// Request DTO para crear o editar una cita médica.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public class CitaRequestDto
{
    /// <summary>Paciente de la cita (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un paciente.")]
    public Guid PacienteId { get; set; }

    /// <summary>Doctor que atiende la cita (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un doctor.")]
    public Guid DoctorId { get; set; }

    /// <summary>Sala donde se realiza la cita (opcional).</summary>
    public Guid? SalaId { get; set; }

    /// <summary>Fecha de la cita (obligatorio). Formato ISO 8601: yyyy-MM-dd.</summary>
    [Required(ErrorMessage = "La fecha de la cita es obligatoria.")]
    public DateOnly FechaCita { get; set; }

    /// <summary>Hora de inicio de la cita (obligatorio). Formato HH:mm:ss.</summary>
    [Required(ErrorMessage = "La hora de la cita es obligatoria.")]
    public TimeOnly HoraCita { get; set; }

    /// <summary>Hora de fin estimada de la cita (opcional).</summary>
    public TimeOnly? HoraFin { get; set; }

    /// <summary>Hora de llegada del paciente (opcional).</summary>
    public TimeOnly? HoraLlegada { get; set; }

    /// <summary>Lugar o sala física de atención.</summary>
    [StringLength(200, ErrorMessage = "El lugar no puede exceder 200 caracteres.")]
    public string? Lugar { get; set; }

    /// <summary>Motivo de la consulta.</summary>
    [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres.")]
    public string? Motivo { get; set; }

    /// <summary>
    /// Estado de la cita. Valores: agendada | cancelada | atendida | en_espera | en_atencion.
    /// Por defecto: "agendada".
    /// </summary>
    [Required(ErrorMessage = "El estado de la cita es obligatorio.")]
    [RegularExpression("^(agendada|cancelada|atendida|en_espera|en_atencion)$",
        ErrorMessage = "Estado no válido. Use: agendada, cancelada, atendida, en_espera o en_atencion.")]
    public string Estado { get; set; } = "agendada";

    /// <summary>Notas internas de la cita.</summary>
    [StringLength(1000, ErrorMessage = "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notas { get; set; }

    /// <summary>
    /// Indica si al asignar este doctor a la cita también se reasigna el médico
    /// asignado del paciente (pacientes.doctor_id). Permitido solo cuando el DTO
    /// lo envía el frontend al cambiar el médico tratante desde la Agenda.
    /// </summary>
    public bool CambiarDoctorPaciente { get; set; }
}
