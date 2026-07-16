using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Notificacion;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de notificaciones del sistema.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface INotificacionService
{
    /// <summary>Obtiene notificaciones de la clínica, opcionalmente filtradas por no leídas.</summary>
    Task<ServiceResult<List<NotificacionResponseDto>>> GetAllAsync(Guid clinicaId, bool? soloNoLeidas = null, int? limit = null);

    /// <summary>Obtiene la cantidad de notificaciones no leídas.</summary>
    Task<ServiceResult<int>> GetNoLeidasCountAsync(Guid clinicaId);

    /// <summary>Marca una notificación específica como leída.</summary>
    Task<ServiceResult<bool>> MarcarLeidaAsync(Guid clinicaId, Guid notificacionId);

    /// <summary>Marca todas las notificaciones de la clínica como leídas.</summary>
    Task<ServiceResult<bool>> MarcarTodasLeidasAsync(Guid clinicaId);

    /// <summary>Crea una nueva notificación programáticamente.</summary>
    Task<ServiceResult<NotificacionResponseDto>> CreateAsync(Notificacion notificacion);
}
