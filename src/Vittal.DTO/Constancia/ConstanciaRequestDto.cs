using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Constancia;

/// <summary>
/// Request DTO para emitir una nueva constancia médica.
/// Las constancias son documentos legales — una vez creadas NO se modifican, solo se anulan.
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public class ConstanciaRequestDto
{
    /// <summary>Expediente del paciente (obligatorio).</summary>
    [Required(ErrorMessage = "Debe especificar el expediente del paciente.")]
    public Guid ExpedienteId { get; set; }

    /// <summary>Hoja de cita asociada (opcional).</summary>
    public Guid? HojaCitaId { get; set; }

    /// <summary>Doctor que emite la constancia (obligatorio).</summary>
    [Required(ErrorMessage = "Debe especificar el doctor que emite la constancia.")]
    public Guid DoctorId { get; set; }

    /// <summary>Tipo de constancia: ASISTENCIA, INCAPACIDAD, REFERENCIA, JUSTIFICANTE (obligatorio).</summary>
    [Required(ErrorMessage = "Debe especificar el tipo de constancia.")]
    [StringLength(50, ErrorMessage = "El tipo de constancia no puede exceder 50 caracteres.")]
    public string TipoConstancia { get; set; } = string.Empty;

    /// <summary>Contenido/texto completo de la constancia (obligatorio).</summary>
    [Required(ErrorMessage = "La constancia debe tener contenido.")]
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Fecha de emisión (opcional — default: ahora UTC).</summary>
    public DateTime? FechaEmision { get; set; }

    /// <summary>Días de reposo/incapacidad (solo para incapacidades médicas).</summary>
    [Range(1, 365, ErrorMessage = "Los días de reposo deben estar entre 1 y 365.")]
    public int? DiasReposo { get; set; }

    /// <summary>Especialista al que se refiere al paciente (solo para referencias).</summary>
    [StringLength(255, ErrorMessage = "El nombre del especialista referido no puede exceder 255 caracteres.")]
    public string? EspecialistaReferido { get; set; }
}
