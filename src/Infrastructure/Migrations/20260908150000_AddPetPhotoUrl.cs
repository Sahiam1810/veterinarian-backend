using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

// Añade PHOTO_URL a PETS para persistir la foto por URL absoluta.
[DbContext(typeof(VeterinaryDbContext))]
[Migration("20260908150000_AddPetPhotoUrl")]
public partial class AddPetPhotoUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'PETS' AND COLUMN_NAME = 'PHOTO_URL';

                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE PETS ADD (PHOTO_URL NVARCHAR2(500))';
                END IF;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'PETS' AND COLUMN_NAME = 'PHOTO_URL';

                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE PETS DROP COLUMN PHOTO_URL';
                END IF;
            END;
            """);
    }
}
