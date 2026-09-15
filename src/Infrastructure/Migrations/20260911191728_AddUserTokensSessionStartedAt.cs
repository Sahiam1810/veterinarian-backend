using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTokensSessionStartedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable first so existing USER_TOKENS rows do not block the ADD.
            migrationBuilder.AddColumn<DateTime>(
                name: "SESSION_STARTED_AT",
                table: "USER_TOKENS",
                type: "TIMESTAMP",
                nullable: true);

            // Policy: legacy refresh tokens become non-renewable after deploy.
            // Backfill SessionStartedAt 25 hours before CreatedAt so
            // now - SessionStartedAt >= MaxSessionHours (24) for every prior row.
            // Users must log in once after the migration.
            migrationBuilder.Sql(
                """
                UPDATE "USER_TOKENS"
                SET "SESSION_STARTED_AT" = "CREATED_AT" - NUMTODSINTERVAL(25, 'HOUR')
                WHERE "SESSION_STARTED_AT" IS NULL
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SESSION_STARTED_AT",
                table: "USER_TOKENS",
                type: "TIMESTAMP",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SESSION_STARTED_AT",
                table: "USER_TOKENS");
        }
    }
}
