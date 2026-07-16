using FluentValidation;
using Vittal.DTO.Expediente;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar expedientes médicos.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU20 — Gestión de Expedientes
/// </summary>
public class ExpedienteRequestValidator : AbstractValidator<ExpedienteRequestDto>
{
    public ExpedienteRequestValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("Debe seleccionar un paciente.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");

        RuleFor(x => x.NotasGenerales)
            .MaximumLength(2000).WithMessage("No pueden superar 2000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.NotasGenerales));
    }
}
