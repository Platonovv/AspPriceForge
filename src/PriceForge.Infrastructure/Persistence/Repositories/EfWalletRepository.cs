using Microsoft.EntityFrameworkCore;
using Npgsql;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Wallets;
using PriceForge.Domain.Wallets;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfWalletRepository : IWalletRepository
{
    private readonly AppDbContext _dbContext;

    public EfWalletRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);

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
