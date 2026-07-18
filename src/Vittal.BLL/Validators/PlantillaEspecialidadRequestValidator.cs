using FluentValidation;
using Vittal.DTO.Plantillas;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar plantillas de especialidad.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU10 — Gestión de Plantillas
/// </summary>
public class PlantillaEspecialidadRequestValidator : AbstractValidator<PlantillaEspecialidadDTOs.Request>
{
    public PlantillaEspecialidadRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la plantilla es obligatorio.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Los ítems de la plantilla son obligatorios.")
            .Must(items => items!.Count > 0).WithMessage("La plantilla debe tener al menos un ítem.")
            .When(x => x.Items != null);
    }
}
