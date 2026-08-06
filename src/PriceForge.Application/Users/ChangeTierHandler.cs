using MediatR;
using PriceForge.Application.Authentication;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Users;

public sealed class ChangeTierHandler : IRequestHandler<ChangeUserTierCommand>
{
    private readonly IUserRepository _userRepository;

    public ChangeTierHandler(IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        _userRepository = userRepository;
    }

    public async Task Handle(ChangeUserTierCommand request, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetUserByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        user.ChangeTier(request.Tier);

        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
