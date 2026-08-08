namespace Vittal.DTO.Dashboard;

/// <summary>
/// DTO para el gráfico de barras apiladas "Citas por Médico".
/// Cada médico es una barra segmentada por estado de cita: atendidas y pendientes.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardCitaPorMedicoDto
{
    /// <summary>Nombre completo del médico (columna BD: nombres + apellidos).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Cantidad de citas atendidas (estado = 'atendida').</summary>
    public int Atendidas { get; set; }

    /// <summary>Cantidad de citas pendientes (agendada, en_espera, en_atencion).</summary>
    public int Pendientes { get; set; }
}