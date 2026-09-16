using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE users SET email = lower(trim(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
