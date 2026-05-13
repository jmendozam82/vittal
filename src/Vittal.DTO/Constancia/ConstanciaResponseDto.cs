using System;

namespace Vittal.DTO.Constancia;

/// <summary>
/// Response DTO para constancias médicas.
/// Incluye datos de JOIN con doctor y paciente.
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public class ConstanciaResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid ExpedienteId { get; set; }
    public Guid? HojaCitaId { get; set; }
    public Guid DoctorId { get; set; }

    /// <summary>Nombre completo del doctor que emitió la constancia (JOIN).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del paciente (JOIN via expediente).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Tipo de constancia: ASISTENCIA, INCAPACIDAD, REFERENCIA, JUSTIFICANTE.</summary>
    public string TipoConstancia { get; set; } = string.Empty;

    /// <summary>Contenido/texto completo de la constancia.</summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Fecha y hora de emisión.</summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>Días de reposo (solo para incapacidades).</summary>
    public int? DiasReposo { get; set; }

    /// <summary>Especialista referido (solo para referencias).</summary>
    public string? EspecialistaReferido { get; set; }

    /// <summary>true = vigente, false = anulada.</summary>
    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
