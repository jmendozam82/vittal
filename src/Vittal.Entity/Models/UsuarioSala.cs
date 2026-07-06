using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Asignación de doctores a salas/áreas de atención.
/// Tabla: public.usuarios_salas
/// </summary>
public class UsuarioSala
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid SalaId { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades de JOIN (no se persisten directamente) ────────
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioEmail { get; set; } = string.Empty;
    public string SalaNombre { get; set; } = string.Empty;
}
