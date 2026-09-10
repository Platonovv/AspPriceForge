using FluentValidation;

namespace PriceForge.Application.Common.Validation;

public static class PasswordRules
{
    public const int MinLength = 8;

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder.NotEmpty().MinimumLength(MinLength);
}
