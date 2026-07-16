using FluentValidation;
using Vittal.DTO.SignosVitalesHoja;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar registros de signos vitales en una hoja de cita.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class SignosVitalesHojaRequestValidator : AbstractValidator<SignosVitalesHojaRequestDto>
{
    public SignosVitalesHojaRequestValidator()
    {
        RuleFor(x => x.HojaCitaId)
            .NotEmpty().WithMessage("La hoja de cita es obligatoria.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");

        RuleFor(x => x.TipoSignoVitalId)
            .NotEmpty().WithMessage("El tipo de signo vital es obligatorio.");

        RuleFor(x => x.Valor)
            .InclusiveBetween(0M, 999999.99M).WithMessage("Debe estar entre 0 y 999,999.99.");

        RuleFor(x => x.Unidad)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Unidad));
    }
}
