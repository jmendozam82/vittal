namespace Vittal.DTO.Dashboard;

/// <summary>
/// DTO para un indicador KPI del dashboard.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardKpiDto
{
    /// <summary>Título del KPI (ej: "Pacientes del día", "Citas pendientes").</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Valor del KPI como string (ej: "24", "15 min", "85%").</summary>
    public string Valor { get; set; } = string.Empty;

    /// <summary>Nombre del icono representativo (ej: "people", "clock", "check-circle").</summary>
    public string Icono { get; set; } = string.Empty;

    /// <summary>Color del KPI (ej: "primary", "success", "warning", "danger").</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Tendencia: up | down | stable</summary>
    public string Tendencia { get; set; } = "stable";

    /// <summary>Variación porcentual respecto al período anterior.</summary>
    public decimal VariacionPorcentaje { get; set; }
}
