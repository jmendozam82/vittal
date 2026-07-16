using System.ComponentModel.DataAnnotations.Schema;

namespace Vittal.Entity;

/// <summary>
/// Ítems predefinidos (antecedentes y signos vitales) para una plantilla de especialidad global.
/// Tabla: public.plantilla_items
/// CASO ESPECIAL: Tabla global del sistema — NO tiene ClinicaId.
/// Historia de Usuario: HU-E02 — Plantillas de Especialidad
/// </summary>
public class PlantillaItem
{
    public Guid Id { get; set; }
    [Column("plantilla_id")]
    public Guid PlantillaId { get; set; }

    /// <summary>
    /// Valores: 'antecedente' o 'signo_vital'
    /// </summary>
    [Column("tipo_item")]
    public string TipoItem { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Ej: 'sistemico', 'ocular', 'quirurgico'
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Valores: 'boolean', 'texto', 'numero'
    /// </summary>
    [Column("tipo_dato")]
    public string TipoDato { get; set; } = "boolean";

    /// <summary>
    /// Ej: 'mmHg', 'bpm', 'kg' (Aplica más para signos vitales)
    /// </summary>
    public string? Unidad { get; set; }

    [Column("valor_min")]
    public decimal? ValorMin { get; set; }
    [Column("valor_max")]
    public decimal? ValorMax { get; set; }

    [Column("es_obligatorio")]
    public bool EsObligatorio { get; set; } = false;
    public int Orden { get; set; } = 0;

    // Auditoría base
    public bool Activo { get; set; } = true;
    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    [Column("fecha_modificacion")]
    public DateTime? FechaModificacion { get; set; }

    // Relaciones
    public PlantillaEspecialidad? Plantilla { get; set; }
}
