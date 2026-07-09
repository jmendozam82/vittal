using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Alerta;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de alertas de tiempo de espera de pacientes.
/// Detecta automáticamente pacientes que exceden el tiempo máximo de espera configurado.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class AlertaEsperaService : IAlertaEsperaService
{
    private readonly IAlertaEsperaRepository _repository;
    private readonly ICitaRepository _citaRepository;
    private readonly IConfiguracionAlertaService _configService;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<AlertaEsperaService> _logger;

    public AlertaEsperaService(
        IAlertaEsperaRepository repository,
        ICitaRepository citaRepository,
        IConfiguracionAlertaService configService,
        INotificacionService notificacionService,
        ILogger<AlertaEsperaService> logger)
    {
        _repository = repository;
        _citaRepository = citaRepository;
        _configService = configService;
        _notificacionService = notificacionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las alertas de espera de la clínica, opcionalmente filtradas.
    /// </summary>
    public async Task<ServiceResult<List<AlertaEsperaResponseDto>>> GetAllAsync(Guid clinicaId, bool? resuelta = null)
    {
        try
        {
            _logger.LogInformation("Obteniendo alertas de espera para clínica {ClinicaId}", clinicaId);

            var entities = await _repository.GetAllByClinicaIdAsync(clinicaId, resuelta);
            var dtos = entities.Select(MapToDto).ToList();

            return ServiceResult<List<AlertaEsperaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener alertas de espera para clínica {ClinicaId}", clinicaId);
            return ServiceResult<List<AlertaEsperaResponseDto>>.Failure($"Error al obtener alertas: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene las alertas de espera no resueltas.
    /// </summary>
    public async Task<ServiceResult<List<AlertaEsperaResponseDto>>> GetNoResueltasAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo alertas no resueltas para clínica {ClinicaId}", clinicaId);

            var entities = await _repository.GetNoResueltasAsync(clinicaId);
            var dtos = entities.Select(MapToDto).ToList();

            return ServiceResult<List<AlertaEsperaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener alertas no resueltas para clínica {ClinicaId}", clinicaId);
            return ServiceResult<List<AlertaEsperaResponseDto>>.Failure($"Error al obtener alertas no resueltas: {ex.Message}");
        }
    }

    /// <summary>
    /// Resuelve una alerta de tiempo de espera manualmente.
    /// </summary>
    public async Task<ServiceResult<bool>> ResolverAlertaAsync(Guid clinicaId, AlertaEsperaResolveDto dto, Guid usuarioId)
    {
        try
        {
            _logger.LogInformation("Resolviendo alerta {AlertaId}", dto.AlertaId);

            var result = await _repository.MarcarResueltaAsync(clinicaId, dto.AlertaId);
            if (!result)
            {
                return ServiceResult<bool>.Failure("Alerta no encontrada o ya resuelta.", ServiceErrorType.NotFound);
            }

            return ServiceResult<bool>.Success(true, "Alerta resuelta exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resolver alerta {AlertaId}", dto.AlertaId);
            return ServiceResult<bool>.Failure($"Error al resolver la alerta: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica los tiempos de espera de todas las citas activas y genera alertas si es necesario.
    /// </summary>
    public async Task<ServiceResult<int>> VerificarTiemposEsperaAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Verificando tiempos de espera para clínica {ClinicaId}", clinicaId);

            // Obtener configuración de alertas
            var configResult = await _configService.GetAsync(clinicaId);
            if (!configResult.IsSuccess || configResult.Data == null)
            {
                return ServiceResult<int>.Failure("No se pudo obtener la configuración de alertas.");
            }

            var config = configResult.Data;
            if (!config.Activo)
            {
                _logger.LogInformation("Alertas deshabilitadas para clínica {ClinicaId}", clinicaId);
                return ServiceResult<int>.Success(0, "Alertas deshabilitadas para esta clínica.");
            }

            // Obtener todas las citas activas de la clínica
            var todasLasCitas = await _citaRepository.GetAllAsync(clinicaId);
            var citasEnEspera = todasLasCitas.Where(c =>
                c.Estado == "en_espera" && c.Activo);

            var alertasGeneradas = 0;
            var horaActual = TimeOnly.FromDateTime(DateTime.UtcNow);

            foreach (var cita in citasEnEspera)
            {
                // Calcular minutos desde la hora de cita hasta ahora
                var minutosEspera = CalcularMinutosDeEspera(cita.HoraCita, horaActual);

                if (minutosEspera >= config.TiempoEsperaMaximoMinutos)
                {
                    // Verificar si ya existe una alerta no resuelta para esta cita
                    var alertasExistentes = await _repository.GetAllByClinicaIdAsync(clinicaId, false);
                    var yaAlertada = alertasExistentes.Any(a => a.CitaId == cita.Id);

                    if (!yaAlertada)
                    {
                        // Crear registro de alerta en alertas_espera
                        var alerta = new AlertaEspera
                        {
                            ClinicaId = clinicaId,
                            CitaId = cita.Id,
                            PacienteId = cita.PacienteId, // requiere acceso: ajustado desde la cita
                            DoctorId = cita.DoctorId,
                            SalaId = cita.SalaId,
                            HoraCita = cita.HoraCita,
                            HoraLlegada = cita.HoraLlegada,
                            MinutosEspera = minutosEspera,
                            Resuelta = false,
                            FechaAlerta = DateTime.UtcNow,
                            PacienteNombre = string.Empty,   // Se completa desde JOIN en consultas posteriores
                            DoctorNombre = string.Empty,
                            SalaNombre = null
                        };
                        var alertaId = await _repository.CreateAsync(alerta);

                        // Crear notificación para el sistema de alertas en tiempo real
                        var notificacion = new Notificacion
                        {
                            ClinicaId = clinicaId,
                            AlertaId = alertaId,
                            Tipo = "alerta_espera",
                            Titulo = "Paciente en espera excede tiempo",
                            Mensaje = $"El paciente ha excedido el tiempo máximo de espera de {config.TiempoEsperaMaximoMinutos} minutos.",
                            Icono = "clock",
                            Color = "warning",
                            Leida = false,
                            Activo = true,
                            FechaCreacion = DateTime.UtcNow
                        };

                        await _notificacionService.CreateAsync(notificacion);
                        alertasGeneradas++;

                        _logger.LogInformation(
                            "Alerta generada para cita {CitaId} — {Minutos} minutos de espera (umbral: {Umbral})",
                            cita.Id, minutosEspera, config.TiempoEsperaMaximoMinutos);
                    }
                }
            }

            _logger.LogInformation("{Count} alertas generadas para clínica {ClinicaId}", alertasGeneradas, clinicaId);
            return ServiceResult<int>.Success(alertasGeneradas, $"{alertasGeneradas} alerta(s) generada(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar tiempos de espera para clínica {ClinicaId}", clinicaId);
            return ServiceResult<int>.Failure($"Error al verificar tiempos de espera: {ex.Message}");
        }
    }

    // ── Métodos auxiliares ──────────────────────────────────────────────

    /// <summary>
    /// Calcula los minutos de espera desde la hora de cita hasta ahora.
    /// </summary>
    private static int CalcularMinutosDeEspera(TimeOnly horaCita, TimeOnly horaActual)
    {
        // Si la cita es a las 10:00 y son las 10:35 → 35 minutos de espera
        var diff = horaActual - horaCita;
        if (diff < TimeSpan.Zero)
        {
            // La cita aún no ha llegado su hora
            return 0;
        }
        return (int)diff.TotalMinutes;
    }

    // ── Mapeo AlertaEspera → AlertaEsperaResponseDto ────────────────────

    private static AlertaEsperaResponseDto MapToDto(AlertaEspera entity)
    {
        return new AlertaEsperaResponseDto
        {
            Id = entity.Id,
            CitaId = entity.CitaId,
            PacienteNombre = entity.PacienteNombre,
            DoctorNombre = entity.DoctorNombre,
            SalaNombre = entity.SalaNombre ?? string.Empty,
            MinutosEspera = entity.MinutosEspera,
            Resuelta = entity.Resuelta,
            FechaAlerta = entity.FechaAlerta
        };
    }
}
