using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tier",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "Regular");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tier",
                table: "users");
        }
    }
}
