using MediatR;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Contracts;

namespace PriceForge.Game.Application;

internal sealed class GameModuleApi : IGameModuleApi
{
    private readonly ISender _sender;

    public GameModuleApi(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    public Task<long> GetWalletBalanceAsync(Guid userId, CancellationToken ct)
        => _sender.Send(new GetWalletBalanceQuery(userId), ct);
}
