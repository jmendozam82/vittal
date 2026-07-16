using FluentValidation;
using Vittal.DTO.Constancia;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para emitir constancias médicas.
/// Las constancias son documentos legales — una vez creadas NO se modifican.
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public class ConstanciaRequestValidator : AbstractValidator<ConstanciaRequestDto>
{
    /// <summary>Tipos de constancia válidos.</summary>
    private static readonly string[] TiposValidos = { "ASISTENCIA", "INCAPACIDAD", "REFERENCIA", "JUSTIFICANTE" };

    public ConstanciaRequestValidator()
    {
        RuleFor(x => x.ExpedienteId)
            .NotEmpty().WithMessage("Debe especificar el expediente del paciente.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe especificar el doctor que emite la constancia.");

        RuleFor(x => x.TipoConstancia)
            .NotEmpty().WithMessage("Debe especificar el tipo de constancia.")
            .Must(t => TiposValidos.Contains(t)).WithMessage("Debe ser ASISTENCIA, INCAPACIDAD, REFERENCIA o JUSTIFICANTE.")
            .MaximumLength(50).WithMessage("No puede superar 50 caracteres.");

        RuleFor(x => x.Contenido)
            .NotEmpty().WithMessage("La constancia debe tener contenido.")
            .MaximumLength(10000).WithMessage("No puede superar 10,000 caracteres.");

        RuleFor(x => x.DiasReposo)
            .InclusiveBetween(1, 365).WithMessage("Los días de reposo deben estar entre 1 y 365.")
            .When(x => x.DiasReposo.HasValue);

        RuleFor(x => x.EspecialistaReferido)
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.EspecialistaReferido));
    }
}
