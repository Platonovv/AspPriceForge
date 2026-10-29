using MediatR;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = new Email(command.Email);
        User? userExist = await _userRepository.GetUserByEmailAsync(email, cancellationToken);

        if (userExist != null)
        {
            throw new ConflictException("User already exists");
        }

        string hashPassword = _passwordHasher.Hash(command.Password);
        var user = new User(email, hashPassword, "User");

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
