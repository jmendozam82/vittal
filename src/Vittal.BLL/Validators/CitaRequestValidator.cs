using FluentValidation;
using Vittal.DTO.Cita;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar citas médicas.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU21 — Agenda
/// </summary>
public class CitaRequestValidator : AbstractValidator<CitaRequestDto>
{
    private static readonly string[] EstadosValidos =
        { "agendada", "cancelada", "atendida", "en_espera", "en_atencion" };

    public CitaRequestValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("Debe seleccionar un paciente.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");

        RuleFor(x => x.FechaCita)
            .NotEmpty().WithMessage("La fecha es obligatoria.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("No puede ser en el pasado.");

        RuleFor(x => x.HoraCita)
            .NotEmpty().WithMessage("La hora es obligatoria.");

        RuleFor(x => x.Lugar)
            .MaximumLength(200).WithMessage("No puede superar 200 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Lugar));

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("No puede superar 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Motivo));

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(e => EstadosValidos.Contains(e.ToLower()))
            .WithMessage("Estado no válido. Use: agendada, cancelada, atendida, en_espera o en_atencion.");

        RuleFor(x => x.Notas)
            .MaximumLength(1000).WithMessage("No pueden superar 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Notas));
    }
}
