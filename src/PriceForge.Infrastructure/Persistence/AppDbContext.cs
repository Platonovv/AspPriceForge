using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Brokers;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Categories;
using PriceForge.Domain.Images;
using PriceForge.Domain.Orders;
using PriceForge.Domain.Products;
using PriceForge.Domain.PromoCodes;
using PriceForge.Domain.Promotions;
using PriceForge.Domain.Users;
using PriceForge.Domain.Wallets;

namespace PriceForge.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductClosure> ProductClosures => Set<ProductClosure>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<CategoryPromotion> CategoryPromotions => Set<CategoryPromotion>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<Wallet> Wallets => Set<Wallet>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.HasPostgresExtension("pg_trgm");
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

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
                                                     CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This item was updated by another action. Please refresh and try again.");
        }
    }
}
