using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Clinica;

public class ClinicaRequestDto
{
    [Required(ErrorMessage = "El nombre de la clínica es obligatorio.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public string? Direccion { get; set; }

    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    [StringLength(255, ErrorMessage = "El correo no puede exceder 255 caracteres.")]
    public string? Email { get; set; }

    public string? LogoUrl { get; set; }

    [Range(1, 480, ErrorMessage = "El tiempo de espera debe estar entre 1 y 480 minutos.")]
    public int TiempoEsperaMinutos { get; set; } = 30;

    public string? BdExterna1 { get; set; }
    public string? BdExterna2 { get; set; }
}
