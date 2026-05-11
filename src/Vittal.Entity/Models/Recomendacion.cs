using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Catálogo de recomendaciones médicas predefinidas para incluir en expedientes.
/// Tabla: public.recomendaciones
/// Historia de Usuario: HU16 — Gestión de Recomendaciones
/// </summary>
public class Recomendacion
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Datos de la recomendación ─────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
