using FluentValidation;
using Vittal.DTO.Recomendacion;

namespace Vittal.BLL.Validators;

public class RecomendacionRequestValidator : AbstractValidator<RecomendacionRequestDto>
{
    public RecomendacionRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la recomendación es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
