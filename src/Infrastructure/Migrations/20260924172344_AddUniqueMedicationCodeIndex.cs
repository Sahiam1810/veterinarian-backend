using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueMedicationCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE MEDICATIONS
                SET CODE = UPPER(TRIM(CODE))
                WHERE CODE IS NOT NULL
                  AND TRIM(CODE) IS NOT NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATIONS_CODE",
                table: "MEDICATIONS",
                column: "CODE",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MEDICATIONS_CODE",
                table: "MEDICATIONS");
        }
    }
}
