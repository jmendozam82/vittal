using FluentValidation;
using Vittal.DTO.TipoCirugia;

namespace Vittal.BLL.Validators;

public class TipoCirugiaRequestValidator : AbstractValidator<TipoCirugiaRequestDto>
{
    public TipoCirugiaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo de cirugía es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
