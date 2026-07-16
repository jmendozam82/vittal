using FluentValidation;
using Vittal.DTO.Diagnostico;

namespace Vittal.BLL.Validators;

public class DiagnosticoRequestValidator : AbstractValidator<DiagnosticoRequestDto>
{
    public DiagnosticoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del diagnóstico es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.TipoDiagnosticoId)
            .NotEmpty().WithMessage("Debe seleccionar un tipo de diagnóstico.");

        RuleFor(x => x.CodigoCie10)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .Matches(@"^[A-Z]\d{2}(\.\d{1,2})?$").WithMessage("Formato CIE-10 no válido (ej: E11.9).")
            .When(x => !string.IsNullOrEmpty(x.CodigoCie10));
    }
}
