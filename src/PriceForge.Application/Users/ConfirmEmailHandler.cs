using MediatR;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Authentication;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.TokenGenerators;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Users;

public sealed class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ConfirmEmailHandler> _logger;
    private readonly IUserTokenRepository _userTokenRepository;

    public ConfirmEmailHandler(ILogger<ConfirmEmailHandler> logger,
                               IUserRepository userRepository,
                               IUserTokenRepository userTokenRepository)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(userTokenRepository);

        _logger = logger;
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        string hash = TokenGenerator.CalculateHash(request.Token);

        UserToken? userToken
            = await _userTokenRepository.GetByHashAsync(hash, UserTokenType.ConfirmedEmail, cancellationToken);

        if (userToken == null || !userToken.IsActive)
        {
            throw new BusinessRuleException("The link is invalid or expired; please request the email again.");
        }

        User? user = await _userRepository.GetUserByIdAsync(userToken.UserId, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleException("The link is invalid or expired; please request the email again.");
        }

        user.EmailConfirmation();
        userToken.MarkUsed();

        await _userTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
