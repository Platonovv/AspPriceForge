using System.Security.Claims;

namespace PriceForge.BuildingBlocks.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        string? claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out Guid guid) ? guid : throw new UnauthorizedAccessException("Invalid credentials");
    }
}
