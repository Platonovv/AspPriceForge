using System.Security.Claims;

namespace PriceForge.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        string? claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out Guid guid) ? guid : throw new UnauthorizedAccessException("Invalid credentials");
    }
}
