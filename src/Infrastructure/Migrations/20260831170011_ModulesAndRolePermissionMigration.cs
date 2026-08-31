using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModulesAndRolePermissionMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MODULES",
                columns: table => new
                {
                    MODULE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "CLOB", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MODULES", x => x.MODULE_ID);
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSIONS",
                columns: table => new
                {
                    ROLE_PERMISSION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ROLE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
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
                    table.PrimaryKey("PK_ROLE_PERMISSIONS", x => x.ROLE_PERMISSION_ID);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSIONS_MODULES_MODULE_ID",
                        column: x => x.MODULE_ID,
                        principalTable: "MODULES",
                        principalColumn: "MODULE_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSIONS_ROLES_ROLE_ID",
                        column: x => x.ROLE_ID,
                        principalTable: "ROLES",
                        principalColumn: "ROLE_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MODULES_NAME",
                table: "MODULES",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSIONS_MODULE_ID",
                table: "ROLE_PERMISSIONS",
                column: "MODULE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSIONS_ROLE_ID_MODULE_ID",
                table: "ROLE_PERMISSIONS",
                columns: new[] { "ROLE_ID", "MODULE_ID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ROLE_PERMISSIONS");

            migrationBuilder.DropTable(
                name: "MODULES");
        }
    }
}
