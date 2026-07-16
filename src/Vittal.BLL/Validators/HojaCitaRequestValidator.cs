using FluentValidation;
using Vittal.DTO.HojaCita;

namespace Vittal.BLL.Validators;

public class HojaCitaRequestValidator : AbstractValidator<HojaCitaRequestDto>
{
    public HojaCitaRequestValidator()
    {
        RuleFor(x => x.ExpedienteId)
            .NotEmpty().WithMessage("Debe seleccionar un expediente.");

        RuleFor(x => x.CitaId)
            .NotEmpty().WithMessage("Debe seleccionar una cita.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");

        RuleFor(x => x.MotivoConsulta)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.MotivoConsulta));

        RuleFor(x => x.NotasConsulta)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.NotasConsulta));
    }
}
