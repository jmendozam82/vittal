using System;
using System.Threading.Tasks;
using Vittal.DTO.Dashboard;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de dashboard. Combina configuración + KPIs en tiempo real.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public interface IDashboardService
{
    /// <summary>Obtiene la configuración de widgets del dashboard para la clínica.</summary>
    Task<ServiceResult<DashboardConfigResponseDto>> GetConfigAsync(Guid clinicaId);

    /// <summary>Guarda la configuración de widgets del dashboard para la clínica.</summary>
    Task<ServiceResult<DashboardConfigResponseDto>> SaveConfigAsync(DashboardConfigRequestDto dto, Guid clinicaId);

    /// <summary>Obtiene los datos completos del dashboard (configuración + KPIs calculados) para una fecha.</summary>
    Task<ServiceResult<DashboardConfigResponseDto>> GetDashboardDataAsync(Guid clinicaId, DateTime fecha);
}
