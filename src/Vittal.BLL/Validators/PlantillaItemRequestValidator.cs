using FluentValidation;
using Vittal.DTO.Plantillas;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar ítems de plantilla de especialidad.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU10 — Gestión de Plantillas
/// </summary>
public class PlantillaItemRequestValidator : AbstractValidator<PlantillaItemDTOs.Request>
{
    public PlantillaItemRequestValidator()
    {
        RuleFor(x => x.PlantillaId)
            .NotEmpty().WithMessage("La plantilla es obligatoria.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del ítem es obligatorio.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.TipoItem)
            .NotEmpty().WithMessage("El tipo de ítem es obligatorio.");

        RuleFor(x => x.TipoDato)
            .NotEmpty().WithMessage("El tipo de dato es obligatorio.");

        RuleFor(x => x.Orden)
            .GreaterThanOrEqualTo(0).WithMessage("El orden no puede ser negativo.");

        RuleFor(x => x)
            .Must(x => !x.ValorMin.HasValue || !x.ValorMax.HasValue || x.ValorMin <= x.ValorMax)
            .WithMessage("El valor mínimo no puede ser mayor que el valor máximo.");
    }
}
