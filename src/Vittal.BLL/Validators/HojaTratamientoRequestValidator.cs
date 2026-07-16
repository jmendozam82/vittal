using FluentValidation;
using Vittal.DTO.HojaTratamiento;

namespace Vittal.BLL.Validators;

public class HojaTratamientoRequestValidator : AbstractValidator<HojaTratamientoRequestDto>
{
    public HojaTratamientoRequestValidator()
    {
        RuleFor(x => x.HojaCitaId)
            .NotEmpty().WithMessage("Debe seleccionar una hoja de cita.");

        RuleFor(x => x.MedicamentoId)
            .NotEmpty().WithMessage("Debe seleccionar un medicamento o tratamiento.")
            .When(x => !x.TratamientoId.HasValue);

        RuleFor(x => x.TratamientoId)
            .NotEmpty().WithMessage("Debe seleccionar un tratamiento o medicamento.")
            .When(x => !x.MedicamentoId.HasValue);

        RuleFor(x => x.Dosis)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Dosis));

        RuleFor(x => x.Frecuencia)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Frecuencia));

        RuleFor(x => x.Duracion)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Duracion));

        RuleFor(x => x.Instrucciones)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Instrucciones));
    }
}
