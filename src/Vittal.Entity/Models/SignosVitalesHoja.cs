namespace Vittal.Entity.Models;

/// <summary>
/// Registro de signos vitales tomados en una hoja de cita.
/// Tabla: public.signos_vitales_hoja
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class SignosVitalesHoja
{
    public Guid Id { get; set; }

    /// <summary>
    /// Discriminador de tenant (solo para RLS)
    /// </summary>
    public Guid ClinicaId { get; set; }

    /// <summary>
    /// Hoja de cita a la que pertenece este registro de signo vital
    /// </summary>
    public Guid HojaCitaId { get; set; }

    /// <summary>
    /// Discriminador de especialidad (la configuración de signos vitales es por sala)
    /// </summary>
    public Guid SalaId { get; set; }

    /// <summary>
    /// Tipo de signo vital (del catálogo definido por sala)
    /// </summary>
    public Guid TipoSignoVitalId { get; set; }

    /// <summary>
    /// Valor numérico del signo vital
    /// </summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// Unidad de medida (se hereda del tipo de signo vital, pero puede sobreescribirse)
    /// </summary>
    public string? Unidad { get; set; }

    /// <summary>
    /// Indica si el valor está fuera del rango normal definido en el tipo de signo vital
    /// </summary>
    public bool FueraDeRango { get; set; }

    /// <summary>
    /// Fecha y hora en que se tomó el signo vital
    /// </summary>
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Usuario que registró el signo vital
    /// </summary>
    public Guid? RegistradoPor { get; set; }

    // Auditoría base
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // Relaciones
    public Clinica? Clinica { get; set; }
    public HojaCita? HojaCita { get; set; }
    public Sala? Sala { get; set; }
    public TipoSignoVital? TipoSignoVital { get; set; }
    public Usuario? Registrador { get; set; }
}
