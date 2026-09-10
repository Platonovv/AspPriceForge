namespace PriceForge.Application.Users;

public sealed record ResetPasswordEvent(string ResetUrl, string Email);
