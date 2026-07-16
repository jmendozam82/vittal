namespace Vittal.Entity;

/// <summary>
/// Catálogo de cirugías específicas, clasificadas por tipo de cirugía.
/// Tabla: public.cirugias
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// </summary>
public class Cirugia
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid TipoCirugiaId { get; set; }

    // ── Datos de la cirugía ──────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    /// <summary>Nombre del tipo de cirugía (JOIN con tipos_cirugia).</summary>
    public string TipoCirugiaNombre { get; set; } = string.Empty;

    /// <summary>Nombre con tipo de cirugía para displays.</summary>
    public string NombreCompleto => $"{Nombre} ({TipoCirugiaNombre})".Trim();
}
