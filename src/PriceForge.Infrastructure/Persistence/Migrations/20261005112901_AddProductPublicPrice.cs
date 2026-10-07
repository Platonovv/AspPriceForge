using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPublicPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "public_price",
                table: "products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"UPDATE products SET public_price = products.base_price_amount");

            migrationBuilder.CreateIndex(
                name: "IX_products_public_price_id",
                table: "products",
                columns: new[] { "public_price", "id" });


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_public_price_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "public_price",
                table: "products");
        }
    }
}
