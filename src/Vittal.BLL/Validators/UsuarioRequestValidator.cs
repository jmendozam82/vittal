using FluentValidation;
using Vittal.DTO.Usuario;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar usuarios del sistema.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU04 — Gestión de Usuarios
/// </summary>
public class UsuarioRequestValidator : AbstractValidator<UsuarioRequestDto>
{
    public UsuarioRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .Matches(@"^[a-zA-Z0-9_\.\-]+$").WithMessage("Solo letras, números, puntos, guiones y guiones bajos.");

        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("Debe tener al menos 6 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .Matches(@"[A-Z]").WithMessage("Debe contener al menos una mayúscula.")
            .Matches(@"[0-9]").WithMessage("Debe contener al menos un número.")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("Debe seleccionar un perfil.");

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F").WithMessage("Debe ser 'M' o 'F'.")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.TipoDocumentoIdentificacion)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio.")
            .Length(2).WithMessage("Debe tener exactamente 2 caracteres (CC, CR o PA).")
            .Must(t => t == "CC" || t == "CR" || t == "PA")
            .WithMessage("Debe ser CC, CR o PA.");

        RuleFor(x => x.NumeroDocumentoIdentificacion)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .MinimumLength(5).WithMessage("Debe tener al menos 5 caracteres.")
            .MaximumLength(30).WithMessage("No puede superar 30 caracteres.")
            .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("Solo letras, números y guiones.");

        RuleFor(x => x.Celular)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$").WithMessage("Formato no válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

    }
}
