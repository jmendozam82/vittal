using FluentValidation;
using Vittal.DTO.Clinica;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para el provisionamiento completo de una nueva clínica.
/// Valida datos de la clínica + datos del administrador inicial.
/// Historia de Usuario: HU-PC01 — Provisionamiento Automático de Clínica
/// </summary>
public class ClinicaProvisionRequestValidator : AbstractValidator<ClinicaProvisionRequestDto>
{
    public ClinicaProvisionRequestValidator()
    {
        // ── Datos de la clínica ────────────────────────────────────────────
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la clínica es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("No puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Direccion));

        RuleFor(x => x.Telefono)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$").WithMessage("Formato no válido.")
            .When(x => !string.IsNullOrEmpty(x.Telefono));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.TiempoEsperaMinutos)
            .InclusiveBetween(1, 480).WithMessage("Debe estar entre 1 y 480 minutos.");

        // ── Datos del administrador inicial ────────────────────────────────
        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("El email del administrador es obligatorio.")
            .EmailAddress().WithMessage("El formato del email del administrador no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("La contraseña del administrador es obligatoria.")
            .MinimumLength(6).WithMessage("Debe tener al menos 6 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.AdminNombres)
            .NotEmpty().WithMessage("Los nombres del administrador son obligatorios.")
            .MinimumLength(2).WithMessage("Deben tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("No pueden superar 100 caracteres.");

        RuleFor(x => x.AdminApellidos)
            .NotEmpty().WithMessage("Los apellidos del administrador son obligatorios.")
            .MinimumLength(2).WithMessage("Deben tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("No pueden superar 100 caracteres.");

        RuleFor(x => x.AdminUsername)
            .NotEmpty().WithMessage("El username del administrador es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(50).WithMessage("No puede superar 50 caracteres.")
            .Matches(@"^[a-zA-Z0-9_\.\-]+$").WithMessage("Solo letras, números, puntos, guiones y guiones bajos.");

        RuleFor(x => x.AdminCelular)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.AdminCelular));
    }
}
