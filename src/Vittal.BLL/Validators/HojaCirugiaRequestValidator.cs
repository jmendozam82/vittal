using FluentValidation;
using Vittal.DTO.HojaCirugia;

namespace Vittal.BLL.Validators;

public class HojaCirugiaRequestValidator : AbstractValidator<HojaCirugiaRequestDto>
{
    public HojaCirugiaRequestValidator()
    {
        RuleFor(x => x.HojaCitaId)
            .NotEmpty().WithMessage("Debe seleccionar una hoja de cita.");

        RuleFor(x => x.CirugiaId)
            .NotEmpty().WithMessage("Debe seleccionar una cirugía.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Observaciones));
    }
}
