using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Clinica;

/// <summary>
/// DTO para la creación completa de una nueva clínica con su administrador inicial.
/// Incluye datos de la clínica + datos del usuario administrador que se creará.
/// Historia de Usuario: HU-PC01 — Provisionamiento Automático de Clínica
/// Uso exclusivo: Super Admin Global
/// </summary>
public class ClinicaProvisionRequestDto
{
    // ── Datos de la clínica ──────────────────────────────────────────────────

    /// <summary>Nombre de la clínica (único en el sistema).</summary>
    [Required(ErrorMessage = "El nombre de la clínica es obligatorio.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Dirección física de la clínica.</summary>
    [StringLength(500)]
    public string? Direccion { get; set; }

    /// <summary>Teléfono de contacto de la clínica.</summary>
    [StringLength(20)]
    public string? Telefono { get; set; }

    /// <summary>Email corporativo de la clínica.</summary>
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>URL del logo de la clínica.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Tiempo de espera por defecto en minutos (para alertas).</summary>
    [Range(1, 480, ErrorMessage = "El tiempo de espera debe estar entre 1 y 480 minutos.")]
    public int TiempoEsperaMinutos { get; set; } = 30;

    /// <summary>Nombre de la BD externa 1 (integración opcional).</summary>
    public string? BdExterna1 { get; set; }

    /// <summary>Nombre de la BD externa 2 (integración opcional).</summary>
    public string? BdExterna2 { get; set; }

    // ── Datos del administrador inicial de la clínica ─────────────────────────

    /// <summary>Email del administrador (usado como login en Supabase Auth).</summary>
    [Required(ErrorMessage = "El email del administrador es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email del administrador no tiene un formato válido.")]
    [StringLength(255)]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Contraseña del administrador para Supabase Auth.</summary>
    [Required(ErrorMessage = "La contraseña del administrador es obligatoria.")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Nombres del administrador.</summary>
    [Required(ErrorMessage = "Los nombres del administrador son obligatorios.")]
    [StringLength(100, MinimumLength = 2)]
    public string AdminNombres { get; set; } = string.Empty;

    /// <summary>Apellidos del administrador.</summary>
    [Required(ErrorMessage = "Los apellidos del administrador son obligatorios.")]
    [StringLength(100, MinimumLength = 2)]
    public string AdminApellidos { get; set; } = string.Empty;

    /// <summary>Nombre de usuario (username) del administrador en el sistema.</summary>
    [Required(ErrorMessage = "El username del administrador es obligatorio.")]
    [StringLength(50, MinimumLength = 3)]
    public string AdminUsername { get; set; } = string.Empty;

    /// <summary>Teléfono del administrador (opcional).</summary>
    [StringLength(20)]
    public string? AdminCelular { get; set; }
}
