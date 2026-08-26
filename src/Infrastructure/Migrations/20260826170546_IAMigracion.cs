using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IAMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PROVIDER_MODELS_AI",
                columns: table => new
                {
                    PROVIDER_MODEL_AI_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME_PROVIDER_AI = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    BUSINESS_NAME = table.Column<string>(type: "VARCHAR2(200)", nullable: true),
                    WEBSITE = table.Column<string>(type: "VARCHAR2(500)", nullable: true),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROVIDER_MODELS_AI", x => x.PROVIDER_MODEL_AI_ID);
                });

            migrationBuilder.CreateTable(
                name: "AI_MODELS",
                columns: table => new
                {
                    AI_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PROVIDER_MODEL_AI_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME_MODEL = table.Column<string>(type: "VARCHAR2(150)", nullable: false),
                    MODEL_KEY = table.Column<string>(type: "VARCHAR2(150)", nullable: false),
                    INPUT_TOKEN_PRICE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    OUTPUT_TOKEN_PRICE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    MAX_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CONTEXT_WINDOW = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_MODELS", x => x.AI_MODEL_ID);
                    table.ForeignKey(
                        name: "FK_AI_MODELS_PROVIDER_MODELS_AI_PROVIDER_MODEL_AI_ID",
                        column: x => x.PROVIDER_MODEL_AI_ID,
                        principalTable: "PROVIDER_MODELS_AI",
                        principalColumn: "PROVIDER_MODEL_AI_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_MODELS_PROVIDER_MODEL_AI_ID",
                table: "AI_MODELS",
                column: "PROVIDER_MODEL_AI_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_MODELS");

            migrationBuilder.DropTable(
                name: "PROVIDER_MODELS_AI");
        }
    }
}
