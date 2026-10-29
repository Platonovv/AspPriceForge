using Microsoft.EntityFrameworkCore.Storage;
using PriceForge.Game.Application.Transactions;
using PriceForge.Game.Infrastructure.Persistence;

namespace PriceForge.Game.Infrastructure.Transactions;

internal sealed class GameTransactionRunner : IGameTransactionRunner
{
    private readonly GameDbContext _dbContext;

    public GameTransactionRunner(GameDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        T result = await action();

        await transaction.CommitAsync(ct);

        return result;
    }

    public async Task RunAsync(Func<Task> action, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        await action();

        await transaction.CommitAsync(ct);
    }
}
