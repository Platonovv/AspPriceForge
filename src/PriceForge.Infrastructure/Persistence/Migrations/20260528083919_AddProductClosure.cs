using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_closure",
                columns: table => new
                {
                    ancestor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descendant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_closure", x => new { x.ancestor_id, x.descendant_id });
                    table.ForeignKey(
                        name: "FK_product_closure_products_ancestor_id",
                        column: x => x.ancestor_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_closure_products_descendant_id",
                        column: x => x.descendant_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_closure_descendant_id",
                table: "product_closure",
                column: "descendant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_closure_descendant_id_depth",
                table: "product_closure",
                columns: new[] { "descendant_id", "depth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_closure");
        }
    }
}
