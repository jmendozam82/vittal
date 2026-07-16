using FluentValidation;
using Vittal.DTO.HojaRecomendacion;

namespace Vittal.BLL.Validators;

public class HojaRecomendacionRequestValidator : AbstractValidator<HojaRecomendacionRequestDto>
{
    public HojaRecomendacionRequestValidator()
    {
        RuleFor(x => x.HojaCitaId)
            .NotEmpty().WithMessage("Debe seleccionar una hoja de cita.");

        RuleFor(x => x.RecomendacionId)
            .NotEmpty().WithMessage("Debe seleccionar una recomendación.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Observaciones));
    }
}
