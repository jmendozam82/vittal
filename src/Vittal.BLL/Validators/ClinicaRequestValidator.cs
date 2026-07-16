using FluentValidation;
using Vittal.DTO.Clinica;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar clínicas.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public class ClinicaRequestValidator : AbstractValidator<ClinicaRequestDto>
{
    public ClinicaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la clínica es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

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

        RuleFor(x => x.HorarioApertura)
            .MaximumLength(5).WithMessage("El formato de hora debe ser HH:mm.")
            .When(x => !string.IsNullOrEmpty(x.HorarioApertura));

        RuleFor(x => x.HorarioCierre)
            .MaximumLength(5).WithMessage("El formato de hora debe ser HH:mm.")
            .When(x => !string.IsNullOrEmpty(x.HorarioCierre));

        RuleFor(x => x.DiasAtencion)
            .MaximumLength(100).WithMessage("No pueden superar 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.DiasAtencion));
    }
}
