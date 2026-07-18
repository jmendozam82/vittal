using FluentValidation;
using Vittal.DTO.UsuarioSala;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para asignar un doctor a una sala/área.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU10 — Gestión de Salas/Áreas
/// </summary>
public class UsuarioSalaRequestValidator : AbstractValidator<UsuarioSalaRequestDto>
{
    public UsuarioSalaRequestValidator()
    {
        RuleFor(x => x.UsuarioId)
            .NotEmpty().WithMessage("El usuario es obligatorio.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");
    }
}
