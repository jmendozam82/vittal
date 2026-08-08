using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Dashboard;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de dashboard. Combina configuración + KPIs en tiempo real.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IDashboardConfigRepository _configRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDashboardConfigRepository configRepository,
        IDashboardRepository dashboardRepository,
        ILogger<DashboardService> logger)
    {
        _configRepository = configRepository;
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la configuración de widgets del dashboard para la clínica.
    /// Si no existe configuración, retorna valores por defecto.
    /// </summary>
    public async Task<ServiceResult<DashboardConfigResponseDto>> GetConfigAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo configuración de dashboard para clínica {ClinicaId}", clinicaId);

            var config = await _configRepository.GetByClinicaIdAsync(clinicaId);
            if (config != null)
            {
                return ServiceResult<DashboardConfigResponseDto>.Success(MapConfigToDto(config));
            }

            // Retornar configuración por defecto
            var defaultDto = new DashboardConfigResponseDto
            {
                Id = Guid.Empty,
                ClinicaId = clinicaId,
                MostrarPacientesDelDia = true,
                MostrarCitasPendientes = true,
                MostrarPacientesEnEspera = true,
                MostrarTiempoPromedioEspera = true,
                MostrarGraficoCitasPorHora = true,
                MostrarCitasPorMedico = true,
                MostrarUltimasAlertas = true
            };

            return ServiceResult<DashboardConfigResponseDto>.Success(defaultDto, "Usando configuración por defecto.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de dashboard para clínica {ClinicaId}", clinicaId);
            return ServiceResult<DashboardConfigResponseDto>.Failure($"Error al obtener configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Guarda la configuración de widgets del dashboard para la clínica.
    /// </summary>
    public async Task<ServiceResult<DashboardConfigResponseDto>> SaveConfigAsync(DashboardConfigRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Guardando configuración de dashboard para clínica {ClinicaId}", clinicaId);

            var existing = await _configRepository.GetByClinicaIdAsync(clinicaId);

            var entity = new DashboardConfig
            {
                ClinicaId = clinicaId,
                MostrarPacientesDelDia = dto.MostrarPacientesDelDia,
                MostrarCitasPendientes = dto.MostrarCitasPendientes,
                MostrarPacientesEnEspera = dto.MostrarPacientesEnEspera,
                MostrarTiempoPromedioEspera = dto.MostrarTiempoPromedioEspera,
                MostrarGraficoCitasPorHora = dto.MostrarGraficoCitasPorHora,
                MostrarCitasPorMedico = dto.MostrarCitasPorMedico,
                MostrarUltimasAlertas = dto.MostrarUltimasAlertas,
                Layout = dto.Layout
            };

            if (existing != null)
            {
                entity.Id = existing.Id;
                entity.FechaCreacion = existing.FechaCreacion;
                entity.FechaModificacion = DateTime.UtcNow;
            }
            else
            {
                entity.FechaCreacion = DateTime.UtcNow;
            }

            var id = await _configRepository.CreateOrUpdateAsync(entity);

            // Recuperar la entidad guardada
            var saved = await _configRepository.GetByClinicaIdAsync(clinicaId);
            var responseDto = MapConfigToDto(saved ?? entity);
            responseDto.Id = id;

            return ServiceResult<DashboardConfigResponseDto>.Success(responseDto, "Configuración guardada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar configuración de dashboard para clínica {ClinicaId}", clinicaId);
            return ServiceResult<DashboardConfigResponseDto>.Failure($"Error al guardar configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene los datos completos del dashboard (configuración + KPIs calculados) para una fecha.
    /// </summary>
    public async Task<ServiceResult<DashboardConfigResponseDto>> GetDashboardDataAsync(Guid clinicaId, DateTime fecha)
    {
        try
        {
            _logger.LogInformation("Obteniendo datos del dashboard para clínica {ClinicaId} en fecha {Fecha}", clinicaId, fecha);

            // 1. Obtener configuración
            var configResult = await GetConfigAsync(clinicaId);
            if (!configResult.IsSuccess || configResult.Data == null)
            {
                return ServiceResult<DashboardConfigResponseDto>.Failure("No se pudo obtener la configuración del dashboard.");
            }

            var dashboardData = configResult.Data;

            // 2. Calcular KPIs
            var tasks = new List<Task>();

            Task<int>? pacientesDelDiaTask = null;
            Task<int>? citasPendientesTask = null;
            Task<int>? pacientesEnEsperaTask = null;
            Task<double>? tiempoPromedioTask = null;
            Task<IEnumerable<DashboardGraficoDto>>? citasPorHoraTask = null;
            Task<IEnumerable<DashboardCitaPorMedicoDto>>? citasPorMedicoTask = null;

            if (dashboardData.MostrarPacientesDelDia)
                pacientesDelDiaTask = _dashboardRepository.GetPacientesDelDiaAsync(clinicaId, fecha);

            if (dashboardData.MostrarCitasPendientes)
                citasPendientesTask = _dashboardRepository.GetCitasPendientesAsync(clinicaId, fecha);

            if (dashboardData.MostrarPacientesEnEspera)
                pacientesEnEsperaTask = _dashboardRepository.GetPacientesEnEsperaAsync(clinicaId);

            if (dashboardData.MostrarTiempoPromedioEspera)
                tiempoPromedioTask = _dashboardRepository.GetTiempoPromedioEsperaAsync(clinicaId, fecha);

            if (dashboardData.MostrarGraficoCitasPorHora)
                citasPorHoraTask = _dashboardRepository.GetCitasPorHoraAsync(clinicaId, fecha);

            if (dashboardData.MostrarCitasPorMedico)
                citasPorMedicoTask = _dashboardRepository.GetCitasPorMedicoAsync(clinicaId, fecha);

            // 3. Esperar todos los KPIs en paralelo
            if (pacientesDelDiaTask != null) dashboardData.PacientesDelDia = await pacientesDelDiaTask;
            if (citasPendientesTask != null) dashboardData.CitasPendientes = await citasPendientesTask;
            if (pacientesEnEsperaTask != null) dashboardData.PacientesEnEspera = await pacientesEnEsperaTask;
            if (tiempoPromedioTask != null) dashboardData.TiempoPromedioEspera = await tiempoPromedioTask;
            if (citasPorHoraTask != null) dashboardData.CitasPorHora = (await citasPorHoraTask).ToList();
            if (citasPorMedicoTask != null) dashboardData.CitasPorMedico = (await citasPorMedicoTask).ToList();

            _logger.LogInformation("Dashboard data obtenido exitosamente para clínica {ClinicaId}", clinicaId);
            return ServiceResult<DashboardConfigResponseDto>.Success(dashboardData, "Datos del dashboard cargados exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener datos del dashboard para clínica {ClinicaId}", clinicaId);
            return ServiceResult<DashboardConfigResponseDto>.Failure($"Error al obtener datos del dashboard: {ex.Message}");
        }
    }

    // ── Mapeo Entity → DTO ──────────────────────────────────────────────

    private static DashboardConfigResponseDto MapConfigToDto(DashboardConfig entity)
    {
        return new DashboardConfigResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            MostrarPacientesDelDia = entity.MostrarPacientesDelDia,
            MostrarCitasPendientes = entity.MostrarCitasPendientes,
            MostrarPacientesEnEspera = entity.MostrarPacientesEnEspera,
            MostrarTiempoPromedioEspera = entity.MostrarTiempoPromedioEspera,
            MostrarGraficoCitasPorHora = entity.MostrarGraficoCitasPorHora,
            MostrarCitasPorMedico = entity.MostrarCitasPorMedico,
            MostrarUltimasAlertas = entity.MostrarUltimasAlertas,
            Layout = entity.Layout
        };
    }

    /// <summary>
    /// Crea un DTO de KPI para usarlo en respuestas agregadas.
    /// </summary>
    private static DashboardKpiDto CrearKpi(string titulo, string valor, string icono, string color, decimal variacion = 0)
    {
        return new DashboardKpiDto
        {
            Titulo = titulo,
            Valor = valor,
            Icono = icono,
            Color = color,
            Tendencia = variacion > 0 ? "up" : variacion < 0 ? "down" : "stable",
            VariacionPorcentaje = variacion
        };
    }
}
