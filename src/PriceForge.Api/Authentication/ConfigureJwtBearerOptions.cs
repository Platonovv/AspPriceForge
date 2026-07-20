using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PriceForge.Infrastructure.Authentication;

namespace PriceForge.Api.Authentication;

internal sealed class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtSettings _settings;
    private readonly ILogger<ConfigureJwtBearerOptions> _logger;

    public ConfigureJwtBearerOptions(ILogger<ConfigureJwtBearerOptions> logger, IOptions<JwtSettings> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _settings = options.Value;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
        };
        options.Events = new JwtBearerEvents {OnTokenValidated = CheckValidAccessToken};
    }

    public void Configure(JwtBearerOptions options) => Configure(null, options);

    private async Task CheckValidAccessToken(TokenValidatedContext context)
    {
        Claim? claimJti = context.Principal?.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);

        if (claimJti != null)
        {
            bool isTokenValid = await IsTokenValid(context, claimJti);

            if (!isTokenValid)
            {
                context.Fail("Invalid access token");
            }
        }
    }

    private async Task<bool> IsTokenValid(TokenValidatedContext context, Claim claimJti)
    {
        try
        {
            IDistributedCache redis = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

            string? cachedAccessToken
                = await redis.GetStringAsync($"revoked:jti:{claimJti.Value}", context.HttpContext.RequestAborted);

            return cachedAccessToken == null;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error getting access token");
        }

        return true;
    }
}
