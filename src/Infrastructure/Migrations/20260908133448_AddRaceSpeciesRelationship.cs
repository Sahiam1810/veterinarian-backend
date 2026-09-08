using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceSpeciesRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SPECIES_ID",
                table: "RACES",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.Sql(
                """
                MERGE INTO "SPECIES" target
                USING (
                    SELECT '88000000-0000-0000-0000-000000000001' AS "SPECIES_ID",
                           'Perro' AS "NAME" FROM DUAL UNION ALL
                    SELECT '88000000-0000-0000-0000-000000000002',
                           'Gato' FROM DUAL UNION ALL
                    SELECT '88000000-0000-0000-0000-000000000003',
                           'Otro' FROM DUAL
                ) source
                ON (UPPER(target."NAME") = UPPER(source."NAME"))
                WHEN NOT MATCHED THEN
                    INSERT ("SPECIES_ID", "NAME", "CREATED_AT")
                    VALUES (source."SPECIES_ID", source."NAME", SYSTIMESTAMP)
                """);

            migrationBuilder.Sql(
                """
                UPDATE "RACES"
                SET "SPECIES_ID" = (
                    SELECT MIN("SPECIES_ID")
                    FROM "SPECIES"
                    WHERE UPPER("NAME") = 'OTRO'
                )
                WHERE "SPECIES_ID" IS NULL
                """);

            migrationBuilder.Sql(
                """
                UPDATE "RACES"
                SET "SPECIES_ID" = (
                    SELECT MIN("SPECIES_ID")
                    FROM "SPECIES"
                    WHERE UPPER("NAME") = 'PERRO'
                )
                WHERE LOWER("RACE_ID") IN (
                    '8a000000-0000-0000-0000-000000000001',
                    '8a000000-0000-0000-0000-000000000002',
                    '8a000000-0000-0000-0000-000000000003',
                    '8a000000-0000-0000-0000-000000000004',
                    '8a000000-0000-0000-0000-000000000005'
                )
                """);

            migrationBuilder.Sql(
                """
                UPDATE "RACES"
                SET "SPECIES_ID" = (
                    SELECT MIN("SPECIES_ID")
                    FROM "SPECIES"
                    WHERE UPPER("NAME") = 'GATO'
                )
                WHERE LOWER("RACE_ID") IN (
                    '8a000000-0000-0000-0000-000000000006',
                    '8a000000-0000-0000-0000-000000000007',
                    '8a000000-0000-0000-0000-000000000008',
                    '8a000000-0000-0000-0000-000000000009'
                )
                """);

            migrationBuilder.Sql(
                """
                MERGE INTO "RACES" target
                USING (
                    SELECT "RACE_ID",
                           SUBSTR("NAME", 1, 10) || '-' ||
                           SUBSTR(REPLACE("RACE_ID", '-', ''), 1, 8) AS "UNIQUE_NAME"
                    FROM (
                        SELECT "RACE_ID", "NAME", "SPECIES_ID",
                               ROW_NUMBER() OVER (
                                   PARTITION BY "SPECIES_ID", UPPER("NAME")
                                   ORDER BY "RACE_ID"
                               ) AS duplicate_number
                        FROM "RACES"
                    )
                    WHERE duplicate_number > 1
                ) duplicate
                ON (target."RACE_ID" = duplicate."RACE_ID")
                WHEN MATCHED THEN UPDATE SET target."NAME" = duplicate."UNIQUE_NAME"
                """);

            migrationBuilder.AlterColumn<string>(
                name: "SPECIES_ID",
                table: "RACES",
                type: "VARCHAR2(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RACES_SPECIES_ID_NAME",
                table: "RACES",
                columns: new[] { "SPECIES_ID", "NAME" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RACES_SPECIES_SPECIES_ID",
                table: "RACES",
                column: "SPECIES_ID",
                principalTable: "SPECIES",
                principalColumn: "SPECIES_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RACES_SPECIES_SPECIES_ID",
                table: "RACES");

            migrationBuilder.DropIndex(
                name: "IX_RACES_SPECIES_ID_NAME",
                table: "RACES");

            migrationBuilder.DropColumn(
                name: "SPECIES_ID",
                table: "RACES");
        }
    }
}
