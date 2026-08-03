using Microsoft.EntityFrameworkCore.Storage;
using PriceForge.Application.Transactions;
using PriceForge.Infrastructure.Persistence;

namespace PriceForge.Infrastructure.Transactions;

public class TransactionRunner : ITransactionRunner
{
    private readonly AppDbContext _dbContext;

    public TransactionRunner(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _dbContext = context;
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        T result = await action();

        await transaction.CommitAsync(ct);

        return result;
    }
}
