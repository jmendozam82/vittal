using FluentValidation;
using Vittal.DTO.Tratamiento;

namespace Vittal.BLL.Validators;

public class TratamientoRequestValidator : AbstractValidator<TratamientoRequestDto>
{
    public TratamientoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del tratamiento es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
