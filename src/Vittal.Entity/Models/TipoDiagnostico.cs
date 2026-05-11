using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Catálogo de tipos de diagnóstico (ej: Refractivo, Glaucoma, Retina) por clínica.
/// Tabla: public.tipos_diagnostico
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// </summary>
public class TipoDiagnostico
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Datos del tipo de diagnóstico ─────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
