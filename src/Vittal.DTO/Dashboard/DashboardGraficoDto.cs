namespace Vittal.DTO.Dashboard;

/// <summary>
/// DTO para datos de un gráfico del dashboard (ej: citas por hora del día).
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardGraficoDto
{
    /// <summary>Etiqueta del punto en el eje X (ej: "08:00", "09:00", "Lun").</summary>
    public string Etiqueta { get; set; } = string.Empty;

    /// <summary>Valor del punto en el eje Y.</summary>
    public int Valor { get; set; }

    /// <summary>Color para la barra/punto del gráfico (opcional, ej: "#4F46E5").</summary>
    public string Color { get; set; } = string.Empty;
}
