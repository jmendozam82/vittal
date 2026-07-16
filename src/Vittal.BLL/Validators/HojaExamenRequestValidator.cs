using FluentValidation;
using Vittal.DTO.HojaExamen;

namespace Vittal.BLL.Validators;

public class HojaExamenRequestValidator : AbstractValidator<HojaExamenRequestDto>
{
    public HojaExamenRequestValidator()
    {
        RuleFor(x => x.HojaCitaId)
            .NotEmpty().WithMessage("Debe seleccionar una hoja de cita.");

        RuleFor(x => x.ExamenId)
            .NotEmpty().WithMessage("Debe seleccionar un examen.");

        RuleFor(x => x.Resultado)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Resultado));

        RuleFor(x => x.ArchivoUrl)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.ArchivoUrl));
    }
}
