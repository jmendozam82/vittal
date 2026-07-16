using FluentValidation;
using Vittal.DTO.Permiso;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para la actualización batch de permisos de un perfil.
/// Historia de Usuario: HU05 — Gestión de Permisos
/// </summary>
public class PermisoItemUpdateDtoValidator : AbstractValidator<PermisoItemUpdateDto>
{
    public PermisoItemUpdateDtoValidator()
    {
        RuleFor(x => x.ModuloId)
            .NotEmpty().WithMessage("Debe especificar el módulo.");
    }
}

/// <summary>
/// Validador FluentValidation para la solicitud de actualización de permisos.
/// Historia de Usuario: HU05 — Gestión de Permisos
/// </summary>
public class PermisoUpdateRequestValidator : AbstractValidator<PermisoUpdateRequestDto>
{
    public PermisoUpdateRequestValidator()
    {
        RuleFor(x => x.Permisos)
            .NotEmpty().WithMessage("Debe incluir al menos un permiso.");

        RuleForEach(x => x.Permisos)
            .SetValidator(new PermisoItemUpdateDtoValidator());
    }
}
