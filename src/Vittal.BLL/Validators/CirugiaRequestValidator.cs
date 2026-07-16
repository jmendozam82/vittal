using FluentValidation;
using Vittal.DTO.Cirugia;

namespace Vittal.BLL.Validators;

public class CirugiaRequestValidator : AbstractValidator<CirugiaRequestDto>
{
    public CirugiaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la cirugía es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.TipoCirugiaId)
            .NotEmpty().WithMessage("Debe seleccionar un tipo de cirugía.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
