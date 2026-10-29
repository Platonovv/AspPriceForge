namespace PriceForge.Game.Application.Transactions;

public interface IGameTransactionRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct);
    Task RunAsync(Func<Task> action, CancellationToken ct);
}
