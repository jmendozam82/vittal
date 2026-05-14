using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DAL.Repositories;
using Vittal.DTO.ConfiguracionAlerta;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de configuración de alertas de tiempo de espera por clínica.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class ConfiguracionAlertaService : IConfiguracionAlertaService
{
    private readonly IConfiguracionAlertaRepository _repository;
    private readonly IClinicaRepository _clinicaRepository;
    private readonly ILogger<ConfiguracionAlertaService> _logger;

    public ConfiguracionAlertaService(
        IConfiguracionAlertaRepository repository,
        IClinicaRepository clinicaRepository,
        ILogger<ConfiguracionAlertaService> logger)
    {
        _repository = repository;
        _clinicaRepository = clinicaRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la configuración de alertas de la clínica.
    /// Si no existe configuración específica, usa Clinica.TiempoEsperaMinutos como fallback.
    /// </summary>
    public async Task<ServiceResult<ConfiguracionAlertaResponseDto>> GetAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo configuración de alertas para clínica {ClinicaId}", clinicaId);

            var config = await _repository.GetByClinicaIdAsync(clinicaId);
            if (config != null)
            {
                return ServiceResult<ConfiguracionAlertaResponseDto>.Success(MapToDto(config));
            }

            // Fallback: usar TiempoEsperaMinutos de la clínica
            _logger.LogInformation("No se encontró configuración de alertas para clínica {ClinicaId}. Usando fallback.", clinicaId);

            var clinica = await _clinicaRepository.GetByIdAsync(clinicaId);
            if (clinica == null)
            {
                return ServiceResult<ConfiguracionAlertaResponseDto>.Failure("Clínica no encontrada.", ServiceErrorType.NotFound);
            }

            var fallbackDto = new ConfiguracionAlertaResponseDto
            {
                Id = Guid.Empty,
                ClinicaId = clinicaId,
                TiempoEsperaMaximoMinutos = clinica.TiempoEsperaMinutos > 0 ? clinica.TiempoEsperaMinutos : 30,
                Activo = false,
                NotificacionSonido = false,
                IntervaloRevisionSegundos = 30,
                FechaCreacion = DateTime.UtcNow
            };

            return ServiceResult<ConfiguracionAlertaResponseDto>.Success(fallbackDto, "Usando configuración por defecto de la clínica.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de alertas para clínica {ClinicaId}", clinicaId);
            return ServiceResult<ConfiguracionAlertaResponseDto>.Failure($"Error al obtener la configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Guarda (crea o actualiza) la configuración de alertas de la clínica.
    /// </summary>
    public async Task<ServiceResult<ConfiguracionAlertaResponseDto>> SaveAsync(ConfiguracionAlertaRequestDto dto, Guid clinicaId, Guid usuarioId)
    {
        try
        {
            _logger.LogInformation("Guardando configuración de alertas para clínica {ClinicaId}", clinicaId);

            var existing = await _repository.GetByClinicaIdAsync(clinicaId);

            var entity = new ConfiguracionAlerta
            {
                ClinicaId = clinicaId,
                TiempoEsperaMaximoMinutos = dto.TiempoEsperaMaximoMinutos,
                Activo = dto.Activo,
                NotificacionSonido = dto.NotificacionSonido,
                IntervaloRevisionSegundos = dto.IntervaloRevisionSegundos
            };

            if (existing != null)
            {
                entity.Id = existing.Id;
                entity.FechaCreacion = existing.FechaCreacion;
                entity.CreadoPor = existing.CreadoPor;
                entity.ModificadoPor = usuarioId;
                entity.FechaModificacion = DateTime.UtcNow;
            }
            else
            {
                entity.CreadoPor = usuarioId;
                entity.FechaCreacion = DateTime.UtcNow;
            }

            var id = await _repository.CreateOrUpdateAsync(entity);

            // Recuperar la entidad guardada para respuesta completa
            var saved = await _repository.GetByClinicaIdAsync(clinicaId);
            var responseDto = MapToDto(saved ?? entity);
            responseDto.Id = id;

            return ServiceResult<ConfiguracionAlertaResponseDto>.Success(responseDto, "Configuración guardada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar configuración de alertas para clínica {ClinicaId}", clinicaId);
            return ServiceResult<ConfiguracionAlertaResponseDto>.Failure($"Error al guardar la configuración: {ex.Message}");
        }
    }

    // ── Mapeo Entity → DTO ──────────────────────────────────────────────

    private static ConfiguracionAlertaResponseDto MapToDto(ConfiguracionAlerta entity)
    {
        return new ConfiguracionAlertaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            TiempoEsperaMaximoMinutos = entity.TiempoEsperaMaximoMinutos,
            Activo = entity.Activo,
            NotificacionSonido = entity.NotificacionSonido,
            IntervaloRevisionSegundos = entity.IntervaloRevisionSegundos,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion,
            CreadoPor = entity.CreadoPor,
            ModificadoPor = entity.ModificadoPor
        };
    }
}
