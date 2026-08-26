using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserTokenMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_TOKENS",
                columns: table => new
                {
                    TOKEN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACCOUNT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TOKEN_VALUE = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: false),
                    TOKEN_TYPE = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_TOKENS", x => x.TOKEN_ID);
                    table.ForeignKey(
                        name: "FK_USER_TOKENS_USER_ACCOUNTS_ACCOUNT_ID",
                        column: x => x.ACCOUNT_ID,
                        principalTable: "USER_ACCOUNTS",
                        principalColumn: "ACCOUNT_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_TOKENS_ACCOUNT_ID",
                table: "USER_TOKENS",
                column: "ACCOUNT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_TOKENS_TOKEN_VALUE",
                table: "USER_TOKENS",
                column: "TOKEN_VALUE",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_TOKENS");
        }
    }
}
