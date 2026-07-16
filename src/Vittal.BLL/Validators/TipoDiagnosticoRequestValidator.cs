using FluentValidation;
using Vittal.DTO.TipoDiagnostico;

namespace Vittal.BLL.Validators;

public class TipoDiagnosticoRequestValidator : AbstractValidator<TipoDiagnosticoRequestDto>
{
    public TipoDiagnosticoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo de diagnóstico es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
