using System.ComponentModel.DataAnnotations;

namespace Vittal.Aplicacion.Models.ViewModels
{
    /// <summary>
    /// ViewModel para el formulario de "Olvidó su contraseña".
    /// El usuario ingresa su email y el sistema notifica al administrador de su clínica.
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido.")]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }
    }
}
