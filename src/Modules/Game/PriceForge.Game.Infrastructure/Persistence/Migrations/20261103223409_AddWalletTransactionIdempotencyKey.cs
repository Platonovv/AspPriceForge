using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Game.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                schema: "game",
                table: "wallet_transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_user_id_idempotency_key",
                schema: "game",
                table: "wallet_transactions",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wallet_transactions_user_id_idempotency_key",
                schema: "game",
                table: "wallet_transactions");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "game",
                table: "wallet_transactions");
        }
    }
}
