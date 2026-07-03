using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Usuario;

/// <summary>
/// Request DTO para crear usuarios por el Super Admin en una clínica específica.
/// A diferencia de UsuarioRequestDto, incluye ClinicaId como campo requerido
/// porque el Super Admin puede crear usuarios en cualquier clínica.
/// </summary>
public class AdminCreateUsuarioRequestDto
{
    [Required(ErrorMessage = "Debe especificar la clínica del usuario.")]
    public Guid ClinicaId { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 100 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Los nombres deben tener entre 3 y 255 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Los apellidos deben tener entre 3 y 255 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Requerida solo en Create. Opcional en Update (si se envía, se actualiza en Supabase Auth).
    /// Mínimo 6 caracteres.
    /// </summary>
    [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Debe asignar un perfil al usuario.")]
    public Guid PerfilId { get; set; }

    /// <summary>M: Masculino, F: Femenino</summary>
    public string? Sexo { get; set; }

    public string? Direccion { get; set; }

    [StringLength(20, ErrorMessage = "El número de celular no puede exceder 20 caracteres.")]
    public string? Celular { get; set; }

    /// <summary>Indica si el usuario es un doctor.</summary>
    public bool EsDoctor { get; set; }
}
