namespace Vittal.Entity;

/// <summary>
/// Configuración del dashboard por clínica.
/// Tabla: public.dashboard_config
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardConfig
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Widgets del dashboard ──────────────────────────────────────
    public bool MostrarPacientesDelDia { get; set; } = true;
    public bool MostrarCitasPendientes { get; set; } = true;
    public bool MostrarPacientesEnEspera { get; set; } = true;
    public bool MostrarTiempoPromedioEspera { get; set; } = true;
    public bool MostrarGraficoCitasPorHora { get; set; } = true;
    public bool MostrarCitasPorMedico { get; set; } = true;
    public bool MostrarUltimasAlertas { get; set; } = true;

    /// <summary>
    /// Layout del dashboard en formato JSON (ej: "{"columnas":3,"orden":["widget1","widget2"]}").
    /// Define la disposición de los widgets en la interfaz.
    /// </summary>
    public string? Layout { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
