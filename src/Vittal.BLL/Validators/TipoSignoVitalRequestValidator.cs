using FluentValidation;
using Vittal.DTO.Catalogos;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar tipos de signo vital.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class TipoSignoVitalRequestValidator : AbstractValidator<TipoSignoVitalDTOs.Request>
{
    public TipoSignoVitalRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo de signo vital es obligatorio.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");

        RuleFor(x => x.Unidad)
            .NotEmpty().WithMessage("La unidad es obligatoria.")
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.");

        RuleFor(x => x)
            .Must(x => !x.ValorMin.HasValue || !x.ValorMax.HasValue || x.ValorMin <= x.ValorMax)
            .WithMessage("El valor mínimo no puede ser mayor que el valor máximo.");

        RuleFor(x => x.Orden)
            .GreaterThanOrEqualTo(0).WithMessage("El orden no puede ser negativo.");
    }
}
