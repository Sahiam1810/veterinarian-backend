using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

// Añade PHOTO_URL a USERS para persistir la foto de perfil por URL absoluta.
[DbContext(typeof(VeterinaryDbContext))]
[Migration("20260915180000_AddUserPhotoUrl")]
public partial class AddUserPhotoUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'USERS' AND COLUMN_NAME = 'PHOTO_URL';

                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE USERS ADD (PHOTO_URL NVARCHAR2(500))';
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
                WHERE TABLE_NAME = 'USERS' AND COLUMN_NAME = 'PHOTO_URL';

                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE USERS DROP COLUMN PHOTO_URL';
                END IF;
            END;
            """);
    }
}
