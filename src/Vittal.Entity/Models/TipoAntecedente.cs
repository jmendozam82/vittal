namespace Vittal.Entity;

/// <summary>
/// Tipos de antecedentes médicos configurados por sala.
/// Tabla: public.tipos_antecedente
/// Historia de Usuario: HU-E03 — Tipos de Antecedente por Sala
/// </summary>
public class TipoAntecedente
{
    public Guid Id { get; set; }

    /// <summary>
    /// Discriminador de tenant (solo para RLS)
    /// </summary>
    public Guid ClinicaId { get; set; }

    /// <summary>
    /// Discriminador de especialidad (la configuración de antecedentes es por sala)
    /// </summary>
    public Guid SalaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }

    /// <summary>
    /// Valores: 'boolean', 'texto', 'numero'
    /// </summary>
    public string TipoDato { get; set; } = "boolean";
    public int Orden { get; set; } = 0;

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
