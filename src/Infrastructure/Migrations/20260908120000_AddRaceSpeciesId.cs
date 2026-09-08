using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

// Añade SPECIES_ID a RACES y rellena según nombre de raza conocido.
public partial class AddRaceSpeciesId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SPECIES_ID",
            table: "RACES",
            type: "VARCHAR2(36)",
            nullable: true);

        // Backfill: razas felinas → Felino/Gato; resto caninas si hay Canino/Perro; fallback primera especie
        migrationBuilder.Sql(
            """
            UPDATE RACES r
            SET SPECIES_ID = (
                SELECT s.SPECIES_ID FROM SPECIES s
                WHERE UPPER(s.NAME) IN ('FELINO', 'GATO')
                  AND ROWNUM = 1
            )
            WHERE UPPER(r.NAME) LIKE '%SIAM%'
               OR UPPER(r.NAME) LIKE '%PERSA%'
               OR UPPER(r.NAME) LIKE '%ANGORA%'
               OR UPPER(r.NAME) LIKE '%BENGAL%'
               OR UPPER(r.NAME) LIKE '%MAINE%'
               OR UPPER(r.NAME) LIKE '%RAGDOLL%'
               OR UPPER(r.NAME) LIKE '%SPHYNX%'
            """);

        migrationBuilder.Sql(
            """
            UPDATE RACES r
            SET SPECIES_ID = (
                SELECT s.SPECIES_ID FROM SPECIES s
                WHERE UPPER(s.NAME) IN ('CANINO', 'PERRO')
                  AND ROWNUM = 1
            )
            WHERE r.SPECIES_ID IS NULL
              AND (
                   UPPER(r.NAME) LIKE '%GOLDEN%'
                OR UPPER(r.NAME) LIKE '%PASTOR%'
                OR UPPER(r.NAME) LIKE '%LABRADOR%'
                OR UPPER(r.NAME) LIKE '%BULLDOG%'
                OR UPPER(r.NAME) LIKE '%BEAGLE%'
                OR UPPER(r.NAME) LIKE '%HUSKY%'
                OR UPPER(r.NAME) LIKE '%POODLE%'
                OR UPPER(r.NAME) LIKE '%TERRIER%'
                OR UPPER(r.NAME) LIKE '%CHIHUAHUA%'
                OR UPPER(r.NAME) LIKE '%ROTTWEILER%'
                OR UPPER(r.NAME) LIKE '%BOXER%'
                OR UPPER(r.NAME) LIKE '%SCHNAUZER%'
              )
            """);

        // Conejo: si el nombre sugiere lagomorfo
        migrationBuilder.Sql(
            """
            UPDATE RACES r
            SET SPECIES_ID = (
                SELECT s.SPECIES_ID FROM SPECIES s
                WHERE UPPER(s.NAME) = 'CONEJO'
                  AND ROWNUM = 1
            )
            WHERE r.SPECIES_ID IS NULL
              AND (
                   UPPER(r.NAME) LIKE '%HOLAND%'
                OR UPPER(r.NAME) LIKE '%REX%'
                OR UPPER(r.NAME) LIKE '%LEON%'
                OR UPPER(r.NAME) LIKE '%ENANO%'
              )
            """);

        // Resto (mestizo, etc.): Canino/Perro o primera especie disponible
        migrationBuilder.Sql(
            """
            UPDATE RACES r
            SET SPECIES_ID = NVL(
                (SELECT s.SPECIES_ID FROM SPECIES s
                 WHERE UPPER(s.NAME) IN ('CANINO', 'PERRO') AND ROWNUM = 1),
                (SELECT s.SPECIES_ID FROM SPECIES s WHERE ROWNUM = 1)
            )
            WHERE r.SPECIES_ID IS NULL
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
            name: "IX_RACES_SPECIES_ID",
            table: "RACES",
            column: "SPECIES_ID");

        migrationBuilder.AddForeignKey(
            name: "FK_RACES_SPECIES_SPECIES_ID",
            table: "RACES",
            column: "SPECIES_ID",
            principalTable: "SPECIES",
            principalColumn: "SPECIES_ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_RACES_SPECIES_SPECIES_ID",
            table: "RACES");

        migrationBuilder.DropIndex(
            name: "IX_RACES_SPECIES_ID",
            table: "RACES");

        migrationBuilder.DropColumn(
            name: "SPECIES_ID",
            table: "RACES");
    }
}
