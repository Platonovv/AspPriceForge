using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_components_products_child_id",
                        column: x => x.child_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_components_products_parent_id",
                        column: x => x.parent_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_components_child_id",
                table: "product_components",
                column: "child_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_components_parent_id_child_id",
                table: "product_components",
                columns: new[] { "parent_id", "child_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_components");
        }
    }
}
