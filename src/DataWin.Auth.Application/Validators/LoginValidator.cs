using DataWin.Auth.Application.Commands.Auth;
using FluentValidation;

namespace DataWin.Auth.Application.Validators;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.IpAddress).NotEmpty();
        RuleFor(x => x.DeviceFingerprint).NotEmpty();
    }
}
