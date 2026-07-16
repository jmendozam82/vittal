using FluentValidation;
using Vittal.DTO.Paciente;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para crear o editar pacientes.
/// Complementa las DataAnnotations del DTO con reglas server-side adicionales.
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public class PacienteRequestValidator : AbstractValidator<PacienteRequestDto>
{
    public PacienteRequestValidator()
    {
        RuleFor(x => x.PrimerNombre)
            .NotEmpty().WithMessage("El primer nombre es obligatorio.")
            .MinimumLength(2).WithMessage("Debe tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$").WithMessage("Solo permite letras.");

        RuleFor(x => x.PrimerApellido)
            .NotEmpty().WithMessage("El primer apellido es obligatorio.")
            .MinimumLength(2).WithMessage("Debe tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$").WithMessage("Solo permite letras.");

        RuleFor(x => x.SegundoNombre)
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SegundoNombre));

        RuleFor(x => x.SegundoApellido)
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SegundoApellido));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Celular)
            .MaximumLength(20).WithMessage("No puede superar 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$").WithMessage("El número no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F").WithMessage("Debe ser 'M' (Masculino) o 'F' (Femenino).")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar el doctor responsable.");

        RuleFor(x => x.TipoDocumentoIdentificacion)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio.")
            .Length(2).WithMessage("Debe tener exactamente 2 caracteres (CC, CR o PA).")
            .Must(t => t == "CC" || t == "CR" || t == "PA")
            .WithMessage("Debe ser CC, CR o PA.");

        RuleFor(x => x.NumeroDocumentoIdentificacion)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .MinimumLength(5).WithMessage("Debe tener al menos 5 caracteres.")
            .MaximumLength(30).WithMessage("No puede superar 30 caracteres.")
            .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("Solo letras, números y guiones.");

        RuleFor(x => x.FechaNacimiento)
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Debe ser anterior a hoy.")
            .GreaterThan(new DateOnly(1900, 1, 1)).WithMessage("La fecha no es válida.")
            .When(x => x.FechaNacimiento.HasValue);

    }
}
