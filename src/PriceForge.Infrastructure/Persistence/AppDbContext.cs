using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Brokers;
using PriceForge.Domain.Products;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductClosure> ProductClosures => Set<ProductClosure>();

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        //1
        var newProductIds = ChangeTracker.Entries<Product>()
                                         .Where(e => e.State == EntityState.Added)
                                         .Select(e => e.Entity.Id)
                                         .ToList();
        //2
        var newEdges = ChangeTracker.Entries<ProductComponent>()
                                    .Where(e => e.State == EntityState.Added)
                                    .Select(e => new {e.Entity.ParentId, e.Entity.ChildId})
                                    .ToList();
        //3
        foreach (Guid pid in newProductIds)
        {
            ProductClosures.Add(new ProductClosure {AncestorId = pid, DescendantId = pid, Depth = 0});
        }

        //4
        int result = await base.SaveChangesAsync(cancellationToken);
        //5
        foreach (var edge in newEdges)
        {
            await Database.ExecuteSqlInterpolatedAsync($"""
                                                        INSERT INTO product_closure (ancestor_id, descendant_id, depth)
                                                        SELECT super.ancestor_id, sub.descendant_id, super.depth + sub.depth + 1
                                                        FROM product_closure AS super
                                                        CROSS JOIN product_closure AS sub
                                                        WHERE super.descendant_id = {edge.ParentId}
                                                          AND sub.ancestor_id = {edge.ChildId}
                                                        """,
                                                       cancellationToken);
        }

        //6
        return result;
    }
}
