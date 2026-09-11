namespace PriceForge.Application.Brokers;

public static class IntegrationEventTypes
{
    public const string ProductCreated = "product.created";
    public const string ProductPriceChanged = "product.price.changed";
    public const string OrderPlaced = "order.placed";
    public const string EmailConfirmationRequested = "email.confirmation.requested";
    public const string PasswordResetRequested = "email.password_reset.requested";
}
