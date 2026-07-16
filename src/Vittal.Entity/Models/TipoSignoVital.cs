namespace Vittal.Entity;

/// <summary>
/// Catálogo de tipos de signos vitales configurado por sala.
/// Tabla: public.tipos_signo_vital
/// Historia de Usuario: HU-E04 — Tipos de Signo Vital por Sala
/// </summary>
public class TipoSignoVital
{
    public Guid Id { get; set; }

    /// <summary>
    /// Discriminador de tenant (solo para RLS)
    /// </summary>
    public Guid ClinicaId { get; set; }

    /// <summary>
    /// Discriminador de especialidad (la configuración de signos vitales es por sala)
    /// </summary>
    public Guid SalaId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Ej: 'mmHg', 'bpm', 'kg', 'm', 'cm', '%', '°C'
    /// </summary>
    public string? Unidad { get; set; }

    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }

    public int Orden { get; set; } = 0;
    public bool EsObligatorio { get; set; } = false;

    // Auditoría base
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }

    // Relaciones
    public Clinica? Clinica { get; set; }
    public Sala? Sala { get; set; }
    public Usuario? Creador { get; set; }
}
