using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageTypesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MESSAGE_TYPES",
                columns: table => new
                {
                    MESSAGE_TYPES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME_TYPE = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MESSAGE_TYPES", x => x.MESSAGE_TYPES_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MESSAGE_TYPES");
        }
    }
}
