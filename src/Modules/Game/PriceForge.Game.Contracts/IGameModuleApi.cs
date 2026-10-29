namespace PriceForge.Game.Contracts;

public interface IGameModuleApi
{
    Task<long> GetWalletBalanceAsync(Guid userId, CancellationToken ct);
}
