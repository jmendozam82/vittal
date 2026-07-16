using FluentValidation;
using Vittal.DTO.HojaDiagnostico;

namespace Vittal.BLL.Validators;

public class HojaDiagnosticoRequestValidator : AbstractValidator<HojaDiagnosticoRequestDto>
{
    public HojaDiagnosticoRequestValidator()
    {
        RuleFor(x => x.HojaCitaId)
            .NotEmpty().WithMessage("Debe seleccionar una hoja de cita.");

        RuleFor(x => x.DiagnosticoId)
            .NotEmpty().WithMessage("Debe seleccionar un diagnóstico.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Observaciones));
    }
}
