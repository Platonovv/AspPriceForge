using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPriceAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE TABLE IF NOT EXISTS product_price_audit ( id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY, product_id uuid REFERENCES products(id), old_price numeric(10,2), new_price numeric(10,2), changed timestamptz DEFAULT now());");
            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION audit_product_price() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN INSERT INTO product_price_audit (product_id, old_price, new_price) VALUES (NEW.id, OLD.base_price_amount, NEW.base_price_amount);RETURN NEW; END; $$;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_product_price ON products;");
            migrationBuilder.Sql(
                "CREATE TRIGGER trg_audit_product_price AFTER UPDATE ON products FOR EACH ROW WHEN (OLD.base_price_amount IS DISTINCT FROM NEW.base_price_amount) EXECUTE FUNCTION audit_product_price();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_product_price ON products;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS audit_product_price();");
            migrationBuilder.Sql("DROP TABLE IF EXISTS product_price_audit;");
        }
    }
}
