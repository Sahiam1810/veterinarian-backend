using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsersCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_CREDENTIALS",
                columns: table => new
                {
                    CREDENTIAL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACCOUNT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PASSWORD_HASH = table.Column<string>(type: "VARCHAR2(255)", nullable: false),
                    LAST_CHANGED = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_CREDENTIALS", x => x.CREDENTIAL_ID);
                    table.ForeignKey(
                        name: "FK_USER_CREDENTIALS_USER_ACCOUNTS_ACCOUNT_ID",
                        column: x => x.ACCOUNT_ID,
                        principalTable: "USER_ACCOUNTS",
                        principalColumn: "ACCOUNT_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_CREDENTIALS_ACCOUNT_ID",
                table: "USER_CREDENTIALS",
                column: "ACCOUNT_ID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_CREDENTIALS");
        }
    }
}
