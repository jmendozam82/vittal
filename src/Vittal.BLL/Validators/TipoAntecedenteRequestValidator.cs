using FluentValidation;
using Vittal.DTO.Catalogos;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar tipos de antecedente.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU-E05 — Antecedentes del Paciente
/// </summary>
public class TipoAntecedenteRequestValidator : AbstractValidator<TipoAntecedenteDTOs.Request>
{
    public TipoAntecedenteRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo de antecedente es obligatorio.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");

        RuleFor(x => x.TipoDato)
            .NotEmpty().WithMessage("El tipo de dato es obligatorio.");

        RuleFor(x => x.Orden)
            .GreaterThanOrEqualTo(0).WithMessage("El orden no puede ser negativo.");
    }
}
