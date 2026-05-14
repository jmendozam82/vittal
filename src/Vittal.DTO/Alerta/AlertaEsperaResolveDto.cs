using System;

namespace Vittal.DTO.Alerta;

/// <summary>
/// Request DTO para resolver una alerta de tiempo de espera.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class AlertaEsperaResolveDto
{
    /// <summary>ID de la alerta a resolver.</summary>
    public Guid AlertaId { get; set; }

    /// <summary>Notas opcionales sobre la resolución de la alerta.</summary>
    public string? NotasResolucion { get; set; }
}
