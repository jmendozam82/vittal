using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Usuario;

/// <summary>
/// Request DTO para que el usuario edite su propio perfil.
/// Solo expone campos editables por el usuario (NO perfil_id, NO es_doctor).
/// </summary>
public class MiPerfilUpdateRequestDto
{
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(255, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(255, MinimumLength = 2)]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>M: Masculino, F: Femenino</summary>
    public string? Sexo { get; set; }

    [StringLength(20)]
    public string? Celular { get; set; }

    public string? Direccion { get; set; }

    /// <summary>URL del avatar en Supabase Storage (bucket: avatares).</summary>
    public string? FotoUrl { get; set; }
}
