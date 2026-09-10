using FluentValidation;
using PriceForge.Application.Common.Validation;

namespace PriceForge.Application.Users;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).Password();
    }
}
