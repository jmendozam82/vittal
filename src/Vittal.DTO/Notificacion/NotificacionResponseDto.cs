using System;

namespace Vittal.DTO.Notificacion;

/// <summary>
/// Response DTO para notificaciones del sistema.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class NotificacionResponseDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Icono { get; set; }
    public string? Color { get; set; }
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }

    /// <summary>Tiempo relativo desde la creación (ej: "hace 5 min", "hace 1 hora").</summary>
    public string TiempoRelativo { get; set; } = string.Empty;
}
