namespace PriceForge.Application.Transactions;

public interface ITransactionRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct);
}
