using FluentValidation;
using PriceForge.Application.Common.Validation;

namespace PriceForge.Application.Authentication;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).Password();
    }
}
