using System;

namespace Vittal.DTO.Notificacion;

/// <summary>
/// Request DTO para marcar notificaciones como leídas.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class NotificacionMarcarLeidaDto
{
    /// <summary>ID de la notificación a marcar como leída (opcional si Todas = true).</summary>
    public Guid? NotificacionId { get; set; }

    /// <summary>Si es true, marca TODAS las notificaciones como leídas (ignora NotificacionId).</summary>
    public bool Todas { get; set; }
}
