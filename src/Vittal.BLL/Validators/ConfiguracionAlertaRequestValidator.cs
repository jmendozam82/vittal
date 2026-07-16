using FluentValidation;
using Vittal.DTO.ConfiguracionAlerta;

namespace Vittal.BLL.Validators;

public class ConfiguracionAlertaRequestValidator : AbstractValidator<ConfiguracionAlertaRequestDto>
{
    public ConfiguracionAlertaRequestValidator()
    {
        RuleFor(x => x.TiempoEsperaMaximoMinutos)
            .GreaterThan(0).WithMessage("Debe ser mayor a 0 minutos.")
            .LessThanOrEqualTo(180).WithMessage("No puede superar 180 minutos.");

        RuleFor(x => x.IntervaloRevisionSegundos)
            .GreaterThanOrEqualTo(10).WithMessage("Debe ser al menos 10 segundos.")
            .LessThanOrEqualTo(300).WithMessage("No puede superar 300 segundos.");
    }
}
