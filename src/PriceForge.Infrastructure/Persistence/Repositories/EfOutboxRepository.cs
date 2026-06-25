using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Brokers;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfOutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _dbContext;

    public EfOutboxRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
        => await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

    public Task<List<OutboxMessage>> GetUnpublishedAsync(CancellationToken ct)
        => _dbContext.OutboxMessages
                     .Where(x => x.PublishedAt == null)
                     .OrderBy(x => x.OccurredAt)
                     .Take(20)
                     .ToListAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);
}
