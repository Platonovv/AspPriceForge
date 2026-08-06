using Microsoft.Extensions.Logging;
using PriceForge.Application.Authentication;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Users;

public sealed class TierService
{
    private readonly ILogger<TierService> _logger;
    private readonly IUserRepository _userRepository;

    public TierService(ILogger<TierService> logger, IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userRepository);

        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task<CustomerTier> GetCustomerTierAsync(Guid userId, CancellationToken cancellationToken)
    {
        CustomerTier? tier = await _userRepository.GetTierByIdAsync(userId, cancellationToken);

        if (tier == null)
        {
            _logger.LogWarning("User {userId} not found.", userId);
            throw new UnauthorizedAccessException($"User {userId} not found.");
        }

        return tier.Value;
    }
}
