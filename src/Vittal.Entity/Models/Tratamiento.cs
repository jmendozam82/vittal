namespace Vittal.Entity;

/// <summary>
/// Catálogo de tratamientos médicos disponibles por clínica para prescripción en expedientes.
/// Tabla: public.tratamientos
/// Historia de Usuario: HU15 — Gestión de Tratamientos
/// </summary>
public class Tratamiento
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Datos del tratamiento ────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
