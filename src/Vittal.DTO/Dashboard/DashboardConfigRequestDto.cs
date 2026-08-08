namespace Vittal.DTO.Dashboard;

/// <summary>
/// Request DTO para configurar los widgets visibles en el dashboard de una clínica.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardConfigRequestDto
{
    public bool MostrarPacientesDelDia { get; set; } = true;
    public bool MostrarCitasPendientes { get; set; } = true;
    public bool MostrarPacientesEnEspera { get; set; } = true;
    public bool MostrarTiempoPromedioEspera { get; set; } = true;
    public bool MostrarGraficoCitasPorHora { get; set; } = true;
    public bool MostrarCitasPorMedico { get; set; } = true;
    public bool MostrarUltimasAlertas { get; set; } = true;

    /// <summary>Layout del dashboard en formato JSON.</summary>
    public string? Layout { get; set; }
}
