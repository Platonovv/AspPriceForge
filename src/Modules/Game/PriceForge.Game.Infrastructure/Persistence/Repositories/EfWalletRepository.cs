using Microsoft.EntityFrameworkCore;
using Npgsql;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Infrastructure.Persistence.Repositories;

internal sealed class EfWalletRepository : IWalletRepository
{
    private readonly GameDbContext _dbContext;

    public EfWalletRepository(GameDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);

    public Task<WalletTransaction?> GetByUserIdAndKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct)
        => _dbContext.WalletTransactions.FirstOrDefaultAsync(
            x => x.UserId == userId && x.IdempotencyKey == idempotencyKey,
            ct);

    public Task<Wallet?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken ct)
        => _dbContext.Wallets
                     .FromSql(
                         $"select user_id, balance, created_at, last_earned_at_utc, xmin from game.wallets where user_id = {userId} for update ")
                     .SingleOrDefaultAsync(ct);

    public async Task AddAsync(Wallet wallet, CancellationToken ct) => await _dbContext.Wallets.AddAsync(wallet, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           })
        {
            throw new ConflictException("Item already exists");
        }
    }
}
