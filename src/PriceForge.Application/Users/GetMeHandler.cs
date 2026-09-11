using MediatR;
using PriceForge.Application.Authentication;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Users;

public sealed class GetMeHandler : IRequestHandler<GetMeQuery, MeDto>
{
    private readonly IUserRepository _userRepository;

    public GetMeHandler(IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);

        _userRepository = userRepository;
    }

    public async Task<MeDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetUserByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        return new MeDto(user.Id, user.Role, user.IsEmailVerified);
    }
}
