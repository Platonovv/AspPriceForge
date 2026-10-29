using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Game.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "game");

            migrationBuilder.CreateTable(
                name: "coin_purchases",
                schema: "game",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coin_price = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coin_purchases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_items",
                schema: "game",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coin_price = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_items", x => x.product_id);
                });

            migrationBuilder.Sql("ALTER TABLE public.wallets SET SCHEMA game;");

            migrationBuilder.DropForeignKey(
                name: "FK_wallets_users_user_id",
                schema: "game",
                table: "wallets");

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                schema: "game",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_wallets_user_id",
                        column: x => x.user_id,
                        principalSchema: "game",
                        principalTable: "wallets",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coin_purchases_product_id",
                schema: "game",
                table: "coin_purchases",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_coin_purchases_user_id_idempotency_key",
                schema: "game",
                table: "coin_purchases",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_user_id_created_at",
                schema: "game",
                table: "wallet_transactions",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.Sql("""
                                 INSERT INTO game.game_items (product_id, coin_price)
                                 VALUES ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', 500),
                                        ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02', 150);
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coin_purchases",
                schema: "game");

            migrationBuilder.DropTable(
                name: "game_items",
                schema: "game");

            migrationBuilder.DropTable(
                name: "wallet_transactions",
                schema: "game");

            migrationBuilder.Sql("ALTER TABLE game.wallets SET SCHEMA public;");

            migrationBuilder.AddForeignKey(
                name: "FK_wallets_users_user_id",
                schema: "public",
                table: "wallets",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
