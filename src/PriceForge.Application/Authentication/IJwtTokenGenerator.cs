namespace PriceForge.Application.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid id, string email, string role);
    RefreshTokenResult GenerateRefreshToken();
}
