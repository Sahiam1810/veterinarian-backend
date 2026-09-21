using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <summary>
/// Frente 1 (T1): CLIENTS gana FULL_NAME, EMAIL e IS_ACTIVE; el teléfono pasa a
/// obligatorio; se elimina REGISTRATION_DATE (redundante con CREATED_AT).
/// </summary>
[DbContext(typeof(VeterinaryDbContext))]
[Migration("20260921140000_AddClientProfileColumns")]
public partial class AddClientProfileColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'FULL_NAME';
                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS ADD (FULL_NAME VARCHAR2(150))';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'EMAIL';
                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS ADD (EMAIL VARCHAR2(150))';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'IS_ACTIVE';
                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS ADD (IS_ACTIVE NUMBER(1) DEFAULT 1 NOT NULL)';
                END IF;
            END;
            """);

        migrationBuilder.Sql(
            """
            BEGIN
                UPDATE CLIENTS c
                SET
                    FULL_NAME = (
                        SELECT u.FULL_NAME FROM USERS u WHERE u.USER_ID = c.USER_ID
                    ),
                    EMAIL = LOWER((
                        SELECT u.EMAIL FROM USERS u WHERE u.USER_ID = c.USER_ID
                    )),
                    IS_ACTIVE = NVL(IS_ACTIVE, 1)
                WHERE FULL_NAME IS NULL OR EMAIL IS NULL;

                UPDATE CLIENTS
                SET FULL_NAME = 'Cliente pendiente',
                    EMAIL = LOWER('pending-' || CLIENT_ID || '@huellitas.local')
                WHERE FULL_NAME IS NULL OR EMAIL IS NULL;

                UPDATE CLIENTS
                SET PHONE_NUMBER = '3000000000'
                WHERE PHONE_NUMBER IS NULL;
            END;
            """);

        migrationBuilder.Sql(
            """
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS'
                  AND COLUMN_NAME = 'FULL_NAME'
                  AND NULLABLE = 'Y';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS MODIFY (FULL_NAME VARCHAR2(150) NOT NULL)';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS'
                  AND COLUMN_NAME = 'EMAIL'
                  AND NULLABLE = 'Y';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS MODIFY (EMAIL VARCHAR2(150) NOT NULL)';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS'
                  AND COLUMN_NAME = 'PHONE_NUMBER'
                  AND NULLABLE = 'Y';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS MODIFY (PHONE_NUMBER NVARCHAR2(20) NOT NULL)';
                END IF;
            END;
            """);

        migrationBuilder.Sql(
            """
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_INDEXES
                WHERE INDEX_NAME = 'UX_CLIENTS_PHONE_NUMBER';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'DROP INDEX UX_CLIENTS_PHONE_NUMBER';
                END IF;

                EXECUTE IMMEDIATE
                    'CREATE UNIQUE INDEX UX_CLIENTS_PHONE_NUMBER ON CLIENTS (PHONE_NUMBER)';

                SELECT COUNT(*) INTO v_count
                FROM USER_INDEXES
                WHERE INDEX_NAME = 'UX_CLIENTS_EMAIL';
                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE
                        'CREATE UNIQUE INDEX UX_CLIENTS_EMAIL ON CLIENTS (EMAIL)';
                END IF;
            END;
            """);

        migrationBuilder.Sql(
            """
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'REGISTRATION_DATE';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS DROP COLUMN REGISTRATION_DATE';
                END IF;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'REGISTRATION_DATE';
                IF v_count = 0 THEN
                    EXECUTE IMMEDIATE
                        'ALTER TABLE CLIENTS ADD (REGISTRATION_DATE TIMESTAMP(7) DEFAULT SYSTIMESTAMP NOT NULL)';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_INDEXES
                WHERE INDEX_NAME = 'UX_CLIENTS_EMAIL';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'DROP INDEX UX_CLIENTS_EMAIL';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_INDEXES
                WHERE INDEX_NAME = 'UX_CLIENTS_PHONE_NUMBER';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'DROP INDEX UX_CLIENTS_PHONE_NUMBER';
                END IF;

                EXECUTE IMMEDIATE
                    'CREATE UNIQUE INDEX UX_CLIENTS_PHONE_NUMBER ON CLIENTS (PHONE_NUMBER)';

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'IS_ACTIVE';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS DROP COLUMN IS_ACTIVE';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'EMAIL';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS DROP COLUMN EMAIL';
                END IF;

                SELECT COUNT(*) INTO v_count
                FROM USER_TAB_COLUMNS
                WHERE TABLE_NAME = 'CLIENTS' AND COLUMN_NAME = 'FULL_NAME';
                IF v_count = 1 THEN
                    EXECUTE IMMEDIATE 'ALTER TABLE CLIENTS DROP COLUMN FULL_NAME';
                END IF;
            END;
            """);
    }
}
