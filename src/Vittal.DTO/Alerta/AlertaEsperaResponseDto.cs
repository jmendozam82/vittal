using System;

namespace Vittal.DTO.Alerta;

/// <summary>
/// Response DTO para alertas de tiempo de espera de pacientes.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class AlertaEsperaResponseDto
{
    public Guid Id { get; set; }
    public Guid CitaId { get; set; }

    /// <summary>Nombre completo del paciente en espera.</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre del doctor asignado a la cita.</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre de la sala donde se atiende.</summary>
    public string SalaNombre { get; set; } = string.Empty;

    /// <summary>Minutos transcurridos desde la hora de cita hasta ahora.</summary>
    public int MinutosEspera { get; set; }

    /// <summary>Indica si la alerta ha sido resuelta.</summary>
    public bool Resuelta { get; set; }

    /// <summary>Fecha y hora en que se generó la alerta.</summary>
    public DateTime FechaAlerta { get; set; }
}
