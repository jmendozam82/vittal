using FluentValidation;
using Vittal.DTO.ExpedienteArchivo;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para subir archivos al expediente médico.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteArchivoRequestValidator : AbstractValidator<ExpedienteArchivoRequestDto>
{
    /// <summary>Tipos MIME permitidos para archivos médicos.</summary>
    private static readonly string[] MimeTypesPermitidos =
    {
        "application/pdf",
        "image/jpeg", "image/png", "image/webp",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public ExpedienteArchivoRequestValidator()
    {
        RuleFor(x => x.ExpedienteId)
            .NotEmpty().WithMessage("Debe seleccionar un expediente.");

        RuleFor(x => x.NombreArchivo)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.");

        RuleFor(x => x.TipoMime)
            .NotEmpty().WithMessage("El tipo MIME es obligatorio.")
            .Must(m => MimeTypesPermitidos.Contains(m.ToLower()))
            .WithMessage("Tipo de archivo no permitido. Use PDF, JPEG, PNG, WebP o DOC/DOCX.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.");

        RuleFor(x => x.StoragePath)
            .NotEmpty().WithMessage("La ruta de almacenamiento es obligatoria.")
            .MaximumLength(500).WithMessage("No puede superar 500 caracteres.");

        RuleFor(x => x.UrlPublica)
            .MaximumLength(1000).WithMessage("No puede superar 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.UrlPublica));

        RuleFor(x => x.TamanoBytes)
            .GreaterThan(0).WithMessage("El tamaño debe ser mayor a 0 bytes.")
            .LessThanOrEqualTo(50_000_000).WithMessage("El archivo no puede superar 50 MB.")
            .When(x => x.TamanoBytes.HasValue);
    }
}
