using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PetsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PETS",
                columns: table => new
                {
                    PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    AGE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    GENDER = table.Column<string>(type: "NVARCHAR2(1)", maxLength: 1, nullable: false),
                    WEIGHT = table.Column<decimal>(type: "NUMBER(6,3)", nullable: false),
                    OBSERVATIONS = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    SPECIES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    RACE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PETS", x => x.PET_ID);
                    table.ForeignKey(
                        name: "FK_PETS_RACES_RACE_ID",
                        column: x => x.RACE_ID,
                        principalTable: "RACES",
                        principalColumn: "RACE_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PETS_SPECIES_SPECIES_ID",
                        column: x => x.SPECIES_ID,
                        principalTable: "SPECIES",
                        principalColumn: "SPECIES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PETS_RACE_ID",
                table: "PETS",
                column: "RACE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PETS_SPECIES_ID",
                table: "PETS",
                column: "SPECIES_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PETS");
        }
    }
}
