using FluentValidation;
using Vittal.DTO.Usuario;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para la edición del perfil propio por parte del usuario.
/// Historia de Usuario: HU04 — Gestión de Usuarios
/// </summary>
public class MiPerfilUpdateRequestValidator : AbstractValidator<MiPerfilUpdateRequestDto>
{
    public MiPerfilUpdateRequestValidator()
    {
        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MinimumLength(2).WithMessage("Deben tener al menos 2 caracteres.")
            .MaximumLength(255).WithMessage("No pueden superar 255 caracteres.");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MinimumLength(2).WithMessage("Deben tener al menos 2 caracteres.")
            .MaximumLength(255).WithMessage("No pueden superar 255 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F").WithMessage("Debe ser 'M' (Masculino) o 'F' (Femenino).")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.Celular)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$").WithMessage("Formato no válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("No puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Direccion));
    }
}
