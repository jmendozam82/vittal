using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Usuarios del sistema, vinculados a Supabase Auth via auth_user_id.
/// Tabla: public.usuarios
/// </summary>
public class Usuario
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid PerfilId { get; set; }
    public Guid? AuthUserId { get; set; }

    // ── Campos de identidad ───────────────────────────────────────
    /// <summary>
    /// Nombre de usuario (login) del sistema. Columna BD: "usuario".
    /// Se mapea con alias en Dapper para evitar conflicto con la clase.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Sexo { get; set; }                  // 'M' | 'F'
    public string? Direccion { get; set; }
    public string? Celular { get; set; }
    public string? FotoUrl { get; set; }
    public bool EsDoctor { get; set; }
    public bool EsSuperAdmin { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    public bool EsAdmin { get; set; }
    public string NombreCompleto => $"{Nombres} {Apellidos}";
    public string PerfilNombre { get; set; } = string.Empty;
}
