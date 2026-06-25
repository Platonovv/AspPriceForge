using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using PriceForge.Application.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PriceForge.Infrastructure.Authentication;

internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _jwtSettings = options.Value;
    }

    public string GenerateAccessToken(Guid id, string email, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()), new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwtSettings.Issuer,
                                         _jwtSettings.Audience,
                                         claims,
                                         DateTime.UtcNow,
                                         DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
                                         credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        string token = Convert.ToBase64String(bytes);
        DateTime expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);
        return new RefreshTokenResult(token, expiresAt);
    }
}
