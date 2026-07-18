using FluentValidation;
using Vittal.DTO.AntecedentesPaciente;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar antecedentes de un paciente.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU-E05 — Antecedentes del Paciente
/// </summary>
public class AntecedentePacienteRequestValidator : AbstractValidator<AntecedentePacienteDTOs.Request>
{
    public AntecedentePacienteRequestValidator()
    {
        RuleFor(x => x.ExpedienteId)
            .NotEmpty().WithMessage("El expediente es obligatorio.");

        RuleFor(x => x.SalaId)
            .NotEmpty().WithMessage("La sala es obligatoria.");

        RuleFor(x => x.TipoAntecedenteId)
            .NotEmpty().WithMessage("El tipo de antecedente es obligatorio.");

        RuleFor(x => x.Valor)
            .NotEmpty().WithMessage("El valor del antecedente es obligatorio.");
    }
}
