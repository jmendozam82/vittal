using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Catálogo de tipos de cirugías (ej: Catarata, LASIK, Pterigión) por clínica.
/// Tabla: public.tipos_cirugia
/// Historia de Usuario: HU11 — Gestión de Tipos de Cirugías
/// </summary>
public class TipoCirugia
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Datos del tipo de cirugía ─────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
