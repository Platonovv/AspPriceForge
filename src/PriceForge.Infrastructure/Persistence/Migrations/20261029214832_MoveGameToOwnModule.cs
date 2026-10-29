using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveGameToOwnModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 INSERT INTO products (id, sku, name, base_price_amount, base_price_currency, public_price)
                                 VALUES ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', 'GAME-GOLDEN-SWORD', 'Golden Sword', 1.99, 'USD', 1.99),
                                        ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02', 'GAME-SPEED-BOOST', 'Speed Boost', 0.99, 'USD', 0.99);

                                 INSERT INTO product_closure (ancestor_id, descendant_id, depth)
                                 VALUES ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', '0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', 0),
                                        ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02', '0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02', 0);
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 DELETE FROM products
                                 WHERE id IN ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', '0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02');
                                 """);
        }
    }
}
