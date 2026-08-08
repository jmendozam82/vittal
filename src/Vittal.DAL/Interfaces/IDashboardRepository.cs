using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Dashboard;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio de solo lectura para consultas de KPIs del dashboard.
/// No tiene operaciones de escritura — los datos provienen de agregaciones en tiempo real.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Obtiene la cantidad de pacientes agendados para el día actual.</summary>
    Task<int> GetPacientesDelDiaAsync(Guid clinicaId, DateTime fecha);

    /// <summary>Obtiene la cantidad de citas pendientes del día.</summary>
    Task<int> GetCitasPendientesAsync(Guid clinicaId, DateTime fecha);

    /// <summary>Obtiene la cantidad de pacientes actualmente en espera.</summary>
    Task<int> GetPacientesEnEsperaAsync(Guid clinicaId);

    /// <summary>Obtiene el tiempo promedio de espera en minutos para una fecha específica.</summary>
    Task<double> GetTiempoPromedioEsperaAsync(Guid clinicaId, DateTime fecha);

    /// <summary>Obtiene la distribución de citas por hora para el día.</summary>
    Task<IEnumerable<DashboardGraficoDto>> GetCitasPorHoraAsync(Guid clinicaId, DateTime fecha);

    /// <summary>Obtiene las citas por médico segmentadas por estado (atendidas / pendientes) para el gráfico apilado.</summary>
    Task<IEnumerable<DashboardCitaPorMedicoDto>> GetCitasPorMedicoAsync(Guid clinicaId, DateTime fecha);
}
