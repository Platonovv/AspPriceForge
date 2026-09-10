namespace PriceForge.Application.Users;

public sealed record SendEmailCreatedEvent(string ConfirmUrl, string Email);
