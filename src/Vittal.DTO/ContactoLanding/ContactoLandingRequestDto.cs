using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.ContactoLanding;

/// <summary>
/// DTO de entrada para el formulario de contacto de la landing page.
/// Validado con FluentValidation en BLL y atributos DataAnnotations para jQuery Validate.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public class ContactoLandingRequestDto
{
    /// <summary>Nombre completo del contactante (requerido, máx. 200 caracteres)</summary>
    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del contactante (requerido, formato válido)</summary>
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(255, ErrorMessage = "El correo no puede exceder 255 caracteres.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Número de teléfono (opcional, máx. 20 caracteres)</summary>
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Rol del contactante (requerido): director, gerente, admin, doctor, otro</summary>
    [Required(ErrorMessage = "Debe seleccionar su rol.")]
    [StringLength(50, ErrorMessage = "El rol no puede exceder 50 caracteres.")]
    public string Rol { get; set; } = string.Empty;

    /// <summary>Mensaje del contactante (requerido, máx. 2000 caracteres)</summary>
    [Required(ErrorMessage = "El mensaje es requerido.")]
    [StringLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;
}
