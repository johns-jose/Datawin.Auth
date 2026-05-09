using DataWin.Auth.Application.Commands.Tenant;
using FluentValidation;

namespace DataWin.Auth.Application.Validators;

public sealed class OnboardTenantValidator : AbstractValidator<OnboardTenantCommand>
{
    public OnboardTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.PrimaryRegion).NotEmpty().MaximumLength(50);
    }
}
