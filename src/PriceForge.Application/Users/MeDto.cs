namespace PriceForge.Application.Users;

public sealed record MeDto(Guid UserId, string UserRole, bool IsEmailVerified);
