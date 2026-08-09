namespace Vittal.DTO.Dashboard;

/// <summary>
/// DTO para el gráfico de barras apiladas "Citas por Hora del Día".
/// Cada hora es una barra segmentada por estado de cita.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardCitaPorHoraDto
{
    /// <summary>Etiqueta de la hora (ej: "08:00", "09:00").</summary>
    public string Etiqueta { get; set; } = string.Empty;

    /// <summary>Cantidad de citas agendadas en esa hora.</summary>
    public int Agendadas { get; set; }

    /// <summary>Cantidad de citas en espera en esa hora.</summary>
    public int EnEspera { get; set; }

    /// <summary>Cantidad de citas en atención en esa hora.</summary>
    public int EnAtencion { get; set; }

    /// <summary>Cantidad de citas atendidas en esa hora.</summary>
    public int Atendidas { get; set; }

    /// <summary>Cantidad de citas canceladas en esa hora.</summary>
    public int Canceladas { get; set; }
}