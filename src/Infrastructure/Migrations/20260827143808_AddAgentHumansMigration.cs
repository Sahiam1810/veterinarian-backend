using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentHumansMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AGENT_HUMANS",
                columns: table => new
                {
                    AGENT_HUMAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AGENT_HUMANS", x => x.AGENT_HUMAN_ID);
                    table.ForeignKey(
                        name: "FK_AGENT_HUMANS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_USER_PROFILES",
                columns: table => new
                {
                    CHAT_USER_PROFILE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    DISPLAY_NAME = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: true),
                    AVATAR_URL = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    BIO = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_USER_PROFILES", x => x.CHAT_USER_PROFILE_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_USER_PROFILES_USERS_PERSON_ID",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AGENT_HUMANS_USER_ID",
                table: "AGENT_HUMANS",
                column: "USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_USER_PROFILES_PERSON_ID",
                table: "CHAT_USER_PROFILES",
                column: "PERSON_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AGENT_HUMANS");

            migrationBuilder.DropTable(
                name: "CHAT_USER_PROFILES");
        }
    }
}
