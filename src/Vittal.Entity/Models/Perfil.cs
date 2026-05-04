using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Perfiles de acceso del sistema por clínica.
/// Tabla: public.perfiles
/// </summary>
public class Perfil
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsAdmin { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades calculadas (no se persisten) ──────────────────
    public int CantidadPermisos { get; set; }
    public int CantidadUsuarios { get; set; }
}
