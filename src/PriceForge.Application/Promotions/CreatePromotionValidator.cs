using FluentValidation;

namespace PriceForge.Application.Promotions;

public sealed class CreatePromotionValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product Id is required");
        RuleFor(x => x.Percent)
            .GreaterThan(0m)
            .LessThanOrEqualTo(90m)
            .WithMessage("Promotion percent must be in range (0, 90]");
        RuleFor(x => x.StartAt).NotEmpty().WithMessage("Start At is required");
        RuleFor(x => x.EndAt).NotEmpty().WithMessage("End At is required");
        RuleFor(command => command.EndAt).GreaterThan(x => x.StartAt).WithMessage("EndAt must be after StartAt");
    }
}
