using FluentValidation;
using Vittal.DTO.Medicamento;

namespace Vittal.BLL.Validators;

public class MedicamentoRequestValidator : AbstractValidator<MedicamentoRequestDto>
{
    public MedicamentoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del medicamento es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Descripcion));

        RuleFor(x => x.Concentracion)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Concentracion));

        RuleFor(x => x.UnidadMedida)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.UnidadMedida));
    }
}
