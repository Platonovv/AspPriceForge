namespace PriceForge.Application.Authentication;

public interface IAccessTokenRevoker
{
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}
