using System;

namespace Vittal.DTO.LineaTiempo;

/// <summary>
/// Request DTO para registrar acciones en la línea de tiempo de una cita.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public class LineaTiempoRequestDto
{
    /// <summary>ID del paso de línea de tiempo sobre el que se realizará la acción.</summary>
    public Guid PasoId { get; set; }

    /// <summary>
    /// Acción a realizar: iniciar | finalizar | saltar
    /// </summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>Observación opcional sobre la acción realizada.</summary>
    public string? Observacion { get; set; }
}
