using FluentValidation;
using Vittal.DTO.Dashboard;

namespace Vittal.BLL.Validators;

public class DashboardConfigRequestValidator : AbstractValidator<DashboardConfigRequestDto>
{
    public DashboardConfigRequestValidator()
    {
        RuleFor(x => x.Layout)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Layout));
    }
}
