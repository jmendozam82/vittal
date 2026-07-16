using FluentValidation;
using Vittal.DTO.Perfil;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar perfiles de usuario.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU03 — Gestión de Perfiles
/// </summary>
public class PerfilRequestValidator : AbstractValidator<PerfilRequestDto>
{
    public PerfilRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del perfil es obligatorio.")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("No puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Descripcion));
    }
}
