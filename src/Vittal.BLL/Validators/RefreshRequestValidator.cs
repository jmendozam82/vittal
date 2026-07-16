using FluentValidation;
using Vittal.DTO.Auth;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para la renovación del token JWT.
/// Historia de Usuario: HU02 — Acceso al Sistema (Login)
/// </summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequestDto>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es obligatorio.")
            .MaximumLength(1000).WithMessage("El token no puede superar 1000 caracteres.");
    }
}
