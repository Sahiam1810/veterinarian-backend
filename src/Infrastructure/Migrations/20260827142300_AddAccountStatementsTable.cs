using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStatementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCOUNT_STATEMENTS",
                columns: table => new
                {
                    STATEMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACCOUNT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ISSUE_DATE = table.Column<DateTime>(type: "DATE", nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCOUNT_STATEMENTS", x => x.STATEMENT_ID);
                    table.ForeignKey(
                        name: "FK_ACCOUNT_STATEMENTS_USER_ACCOUNTS_ACCOUNT_ID",
                        column: x => x.ACCOUNT_ID,
                        principalTable: "USER_ACCOUNTS",
                        principalColumn: "ACCOUNT_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_STATEMENTS_ACCOUNT_ID",
                table: "ACCOUNT_STATEMENTS",
                column: "ACCOUNT_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ACCOUNT_STATEMENTS");
        }
    }
}
