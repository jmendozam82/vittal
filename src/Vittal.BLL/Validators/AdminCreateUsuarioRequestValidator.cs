using FluentValidation;
using Vittal.DTO.Usuario;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para la creación de usuarios por el Super Admin.
/// Incluye ClinicaId como campo requerido para multi-tenant.
/// Historia de Usuario: HU04 — Gestión de Usuarios
/// </summary>
public class AdminCreateUsuarioRequestValidator : AbstractValidator<AdminCreateUsuarioRequestDto>
{
    public AdminCreateUsuarioRequestValidator()
    {
        RuleFor(x => x.ClinicaId)
            .NotEmpty().WithMessage("Debe especificar la clínica del usuario.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .Matches(@"^[a-zA-Z0-9_\.\-]+$").WithMessage("Solo letras, números, puntos, guiones y guiones bajos.");

        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MinimumLength(3).WithMessage("Deben tener al menos 3 caracteres.")
            .MaximumLength(255).WithMessage("No pueden superar 255 caracteres.");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MinimumLength(3).WithMessage("Deben tener al menos 3 caracteres.")
            .MaximumLength(255).WithMessage("No pueden superar 255 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("Debe tener al menos 6 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("Debe asignar un perfil al usuario.");

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F").WithMessage("Debe ser 'M' o 'F'.")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.Celular)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Celular));
    }
}
