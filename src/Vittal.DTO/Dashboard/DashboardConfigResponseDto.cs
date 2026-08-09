using System;
using System.Collections.Generic;
using Vittal.DTO.Notificacion;

namespace Vittal.DTO.Dashboard;

/// <summary>
/// Response DTO con la configuración del dashboard y los KPIs calculados.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardConfigResponseDto
{
    // ── Configuración ──────────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public bool MostrarPacientesDelDia { get; set; }
    public bool MostrarCitasPendientes { get; set; }
    public bool MostrarPacientesEnEspera { get; set; }
    public bool MostrarTiempoPromedioEspera { get; set; }
    public bool MostrarGraficoCitasPorHora { get; set; }
    public bool MostrarCitasPorMedico { get; set; }
    public bool MostrarUltimasAlertas { get; set; }
    public string? Layout { get; set; }

    // ── KPIs calculados ────────────────────────────────────────────
    /// <summary>Cantidad de pacientes agendados para el día actual.</summary>
    public int PacientesDelDia { get; set; }

    /// <summary>Cantidad de citas pendientes por atender.</summary>
    public int CitasPendientes { get; set; }

    /// <summary>Cantidad de pacientes actualmente en espera.</summary>
    public int PacientesEnEspera { get; set; }

    /// <summary>Cantidad de pacientes actualmente en atención.</summary>
    public int PacientesEnAtencion { get; set; }

    /// <summary>Cantidad de citas canceladas del día (estado = cancelada).</summary>
    public int CitasCanceladas { get; set; }

    /// <summary>Tiempo promedio de espera en minutos.</summary>
    public double TiempoPromedioEspera { get; set; }

    /// <summary>Distribución de citas por hora, segmentada por estado para el gráfico apilado.</summary>
    public List<DashboardCitaPorHoraDto> CitasPorHora { get; set; } = new();

    /// <summary>Citas por médico, segmentadas por estado (atendidas / pendientes) para el gráfico apilado.</summary>
    public List<DashboardCitaPorMedicoDto> CitasPorMedico { get; set; } = new();

    /// <summary>Últimas alertas de tiempo de espera.</summary>
    public List<NotificacionResponseDto> UltimasAlertas { get; set; } = new();
}
