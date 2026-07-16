using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Notificacion;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de notificaciones del sistema.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class NotificacionService : INotificacionService
{
    private readonly INotificacionRepository _repository;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(
        INotificacionRepository repository,
        ILogger<NotificacionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene notificaciones de la clínica, opcionalmente filtradas por no leídas.
    /// </summary>
    public async Task<ServiceResult<List<NotificacionResponseDto>>> GetAllAsync(Guid clinicaId, bool? soloNoLeidas = null, int? limit = null)
    {
        try
        {
            _logger.LogInformation("Obteniendo notificaciones para clínica {ClinicaId}", clinicaId);

            var entities = await _repository.GetByClinicaIdAsync(clinicaId, soloNoLeidas, limit);
            var dtos = entities.Select(MapToDto).ToList();

            return ServiceResult<List<NotificacionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener notificaciones para clínica {ClinicaId}", clinicaId);
            return ServiceResult<List<NotificacionResponseDto>>.Failure($"Error al obtener notificaciones: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la cantidad de notificaciones no leídas.
    /// </summary>
    public async Task<ServiceResult<int>> GetNoLeidasCountAsync(Guid clinicaId)
    {
        try
        {
            var count = await _repository.GetNoLeidasCountAsync(clinicaId);
            return ServiceResult<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar notificaciones no leídas para clínica {ClinicaId}", clinicaId);
            return ServiceResult<int>.Failure($"Error al contar notificaciones: {ex.Message}");
        }
    }

    /// <summary>
    /// Marca una notificación específica como leída.
    /// </summary>
    public async Task<ServiceResult<bool>> MarcarLeidaAsync(Guid clinicaId, Guid notificacionId)
    {
        try
        {
            _logger.LogInformation("Marcando notificación {NotificacionId} como leída", notificacionId);

            var result = await _repository.MarcarLeidaAsync(clinicaId, notificacionId);
            if (!result)
            {
                return ServiceResult<bool>.Failure("Notificación no encontrada.", ServiceErrorType.NotFound);
            }

            return ServiceResult<bool>.Success(true, "Notificación marcada como leída.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar notificación {NotificacionId} como leída", notificacionId);
            return ServiceResult<bool>.Failure($"Error al marcar notificación como leída: {ex.Message}");
        }
    }

    /// <summary>
    /// Marca todas las notificaciones de la clínica como leídas.
    /// </summary>
    public async Task<ServiceResult<bool>> MarcarTodasLeidasAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Marcando todas las notificaciones como leídas para clínica {ClinicaId}", clinicaId);

            var result = await _repository.MarcarTodasLeidasAsync(clinicaId);
            return ServiceResult<bool>.Success(result, "Todas las notificaciones fueron marcadas como leídas.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar todas las notificaciones como leídas para clínica {ClinicaId}", clinicaId);
            return ServiceResult<bool>.Failure($"Error al marcar notificaciones: {ex.Message}");
        }
    }

    /// <summary>
    /// Crea una nueva notificación programáticamente.
    /// </summary>
    public async Task<ServiceResult<NotificacionResponseDto>> CreateAsync(Notificacion notificacion)
    {
        try
        {
            _logger.LogInformation("Creando nueva notificación para clínica {ClinicaId}", notificacion.ClinicaId);

            var id = await _repository.CreateAsync(notificacion);
            notificacion.Id = id;

            var dto = MapToDto(notificacion);
            return ServiceResult<NotificacionResponseDto>.Success(dto, "Notificación creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear notificación");
            return ServiceResult<NotificacionResponseDto>.Failure($"Error al crear notificación: {ex.Message}");
        }
    }

    // ── Mapeo Entity → DTO ──────────────────────────────────────────────

    private static NotificacionResponseDto MapToDto(Notificacion entity)
    {
        return new NotificacionResponseDto
        {
            Id = entity.Id,
            Tipo = entity.Tipo,
            Titulo = entity.Titulo,
            Mensaje = entity.Mensaje,
            Icono = entity.Icono,
            Color = entity.Color,
            Leida = entity.Leida,
            FechaCreacion = entity.FechaCreacion,
            TiempoRelativo = FormatearTiempoRelativo(entity.FechaCreacion)
        };
    }

    /// <summary>
    /// Formatea la fecha de creación en tiempo relativo (ej: "hace 5 min", "hace 1 hora").
    /// </summary>
    private static string FormatearTiempoRelativo(DateTime fechaCreacion)
    {
        var diff = DateTime.UtcNow - fechaCreacion;

        if (diff.TotalMinutes < 1)
            return "hace unos segundos";
        if (diff.TotalMinutes < 60)
            return $"hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24)
            return $"hace {(int)diff.TotalHours} hora(s)";
        if (diff.TotalDays < 7)
            return $"hace {(int)diff.TotalDays} día(s)";

        return fechaCreacion.ToString("dd/MM/yyyy");
    }
}
