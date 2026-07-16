using FluentValidation;
using Vittal.DTO.LineaTiempo;

namespace Vittal.BLL.Validators;

public class LineaTiempoRequestValidator : AbstractValidator<LineaTiempoRequestDto>
{
    public LineaTiempoRequestValidator()
    {
        RuleFor(x => x.PasoId)
            .NotEmpty().WithMessage("Debe especificar un paso.");

        RuleFor(x => x.Accion)
            .NotEmpty().WithMessage("La acción es obligatoria.")
            .MaximumLength(50).WithMessage("No puede superar 50 caracteres.");

        RuleFor(x => x.Observacion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Observacion));
    }
}
