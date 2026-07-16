using FluentValidation;
using Vittal.DTO.Sala;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar salas/áreas.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU10 — Gestión de Salas/Áreas
/// </summary>
public class SalaRequestValidator : AbstractValidator<SalaRequestDto>
{
    public SalaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la sala es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("No puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
