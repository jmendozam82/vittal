using FluentValidation;
using Vittal.DTO.Examen;

namespace Vittal.BLL.Validators;

public class ExamenRequestValidator : AbstractValidator<ExamenRequestDto>
{
    public ExamenRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del examen es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
