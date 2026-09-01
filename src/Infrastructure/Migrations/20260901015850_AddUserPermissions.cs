using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_PERMISSIONS",
                columns: table => new
                {
                    USER_PERMISSION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    MODULE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CAN_VIEW = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CAN_CREATE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CAN_EDIT = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CAN_DELETE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_PERMISSIONS", x => x.USER_PERMISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_PERMISSIONS_MODULES_MODULE_ID",
                        column: x => x.MODULE_ID,
                        principalTable: "MODULES",
                        principalColumn: "MODULE_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_PERMISSIONS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_PERMISSIONS_MODULE_ID",
                table: "USER_PERMISSIONS",
                column: "MODULE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_PERMISSIONS_USER_ID_MODULE_ID",
                table: "USER_PERMISSIONS",
                columns: new[] { "USER_ID", "MODULE_ID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_PERMISSIONS");
        }
    }
}
