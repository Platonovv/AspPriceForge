using FluentValidation;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Users;

public sealed class ChangeUserTierValidator : AbstractValidator<ChangeUserTierCommand>
{
    public ChangeUserTierValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Please provide a valid user id");
        RuleFor(x => x.Tier)
            .IsInEnum()
            .WithMessage("Tier must be in enum")
            .NotEqual(CustomerTier.None)
            .WithMessage("Tier must not be None");
    }
}
