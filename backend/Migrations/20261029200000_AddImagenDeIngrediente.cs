using backend.data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261029200000_AddImagenDeIngrediente")]
public sealed class AddImagenDeIngrediente : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS "Ingredientes"
                ADD COLUMN IF NOT EXISTS "ImagenDeIngrediente" text NOT NULL DEFAULT '';
            ALTER TABLE IF EXISTS "Ingredientes"
                ALTER COLUMN "ImagenDeIngrediente" DROP DEFAULT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS "Ingredientes"
                DROP COLUMN IF EXISTS "ImagenDeIngrediente";
            """);
    }
}
