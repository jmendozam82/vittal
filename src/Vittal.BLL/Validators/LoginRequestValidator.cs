using FluentValidation;
using Vittal.DTO.Auth;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para el inicio de sesión (login).
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU02 — Acceso al Sistema (Login)
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");
    }
}
