using FluentValidation;
using Vittal.DTO.Reporte;

namespace Vittal.BLL.Validators;

public class ReporteRequestValidator : AbstractValidator<ReporteRequestDto>
{
    public ReporteRequestValidator()
    {
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de reporte es obligatorio.")
            .MaximumLength(50).WithMessage("No puede superar 50 caracteres.");

        RuleFor(x => x.FechaInicio)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.FechaFin)
            .NotEmpty().WithMessage("La fecha de fin es obligatoria.")
            .GreaterThanOrEqualTo(x => x.FechaInicio)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.Formato)
            .NotEmpty().WithMessage("El formato es obligatorio.")
            .Must(f => f == "PDF" || f == "Excel" || f == "CSV")
            .WithMessage("El formato debe ser PDF, Excel o CSV.");
    }
}
