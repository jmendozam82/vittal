using FluentValidation;
using Vittal.DTO.ContactoLanding;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para el formulario de contacto de la landing.
/// Aplica reglas server-side que complementan las DataAnnotations del DTO.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public class ContactoLandingRequestValidator : AbstractValidator<ContactoLandingRequestDto>
{
    /// <summary>Roles válidos para el campo Rol del contacto</summary>
    private static readonly string[] RolesValidos = { "director", "gerente", "admin", "doctor", "otro" };

    public ContactoLandingRequestValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(255).WithMessage("El correo no puede exceder 255 caracteres.");

        RuleFor(x => x.Telefono)
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres.")
            .Matches(@"^[\d\s\+\-\(\)]*$")
            .WithMessage("El teléfono solo puede contener números, espacios, guiones y paréntesis.");

        RuleFor(x => x.Rol)
            .NotEmpty().WithMessage("Debe seleccionar su rol.")
            .Must(rol => RolesValidos.Contains(rol.ToLower()))
            .WithMessage("El rol seleccionado no es válido.");

        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("El mensaje es requerido.")
            .MaximumLength(2000).WithMessage("El mensaje no puede exceder 2000 caracteres.");
    }
}
