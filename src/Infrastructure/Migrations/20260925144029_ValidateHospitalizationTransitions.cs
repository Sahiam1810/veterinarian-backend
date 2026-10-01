using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    // Idempotente: en Oracle cada DDL hace commit implícito, así que un intento fallido deja
    // pasos aplicados sin registrar la migración. Cada paso comprueba el estado antes de actuar.
    /// <inheritdoc />
    public partial class ValidateHospitalizationTransitions : Migration
    {
        private static readonly string[] OrderTables = ["PROCEDURE_ORDERS", "MEDICATION_ORDERS"];

        // Sin comillas simples: se inserta tal cual dentro de un EXECUTE IMMEDIATE '...'.
        private const string OriginCheckSql =
            "(APPOINTMENT_ID IS NOT NULL AND HOSPITALIZATION_STAY_ID IS NULL) OR "
            + "(APPOINTMENT_ID IS NULL AND HOSPITALIZATION_STAY_ID IS NOT NULL)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in OrderTables)
            {
                // Las órdenes de hospitalización no tienen cita.
                migrationBuilder.Sql(
                    $"""
                    DECLARE
                        v_nullable VARCHAR2(1);
                    BEGIN
                        SELECT NULLABLE INTO v_nullable
                        FROM USER_TAB_COLUMNS
                        WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = 'APPOINTMENT_ID';
                        IF v_nullable = 'N' THEN
                            EXECUTE IMMEDIATE 'ALTER TABLE {table} MODIFY (APPOINTMENT_ID NULL)';
                        END IF;
                    END;
                    """);

                // Origen único: cita o estancia, nunca ambas ni ninguna.
                migrationBuilder.Sql(
                    $"""
                    DECLARE
                        v_count NUMBER;
                    BEGIN
                        SELECT COUNT(*) INTO v_count
                        FROM USER_CONSTRAINTS
                        WHERE TABLE_NAME = '{table}' AND CONSTRAINT_NAME = 'CK_{table}_ORIGIN';
                        IF v_count = 0 THEN
                            EXECUTE IMMEDIATE 'ALTER TABLE {table} ADD CONSTRAINT CK_{table}_ORIGIN CHECK ({OriginCheckSql})';
                        END IF;
                    END;
                    """);

                // FK a la cita con borrado en cascada, como declara el modelo. La rama anterior
                // la recreó sin ON DELETE CASCADE: se normaliza a una única FK con cascada.
                migrationBuilder.Sql(
                    $"""
                    DECLARE
                        v_count NUMBER;
                    BEGIN
                        FOR c IN (
                            SELECT uc.CONSTRAINT_NAME, uc.DELETE_RULE
                            FROM USER_CONSTRAINTS uc
                            JOIN USER_CONS_COLUMNS cc ON cc.CONSTRAINT_NAME = uc.CONSTRAINT_NAME
                            WHERE uc.TABLE_NAME = '{table}'
                              AND uc.CONSTRAINT_TYPE = 'R'
                              AND cc.COLUMN_NAME = 'APPOINTMENT_ID')
                        LOOP
                            IF c.DELETE_RULE <> 'CASCADE'
                                OR c.CONSTRAINT_NAME <> 'FK_{table}_APPOINTMENTS_APPOINTMENT_ID' THEN
                                EXECUTE IMMEDIATE 'ALTER TABLE {table} DROP CONSTRAINT ' || c.CONSTRAINT_NAME;
                            END IF;
                        END LOOP;

                        SELECT COUNT(*) INTO v_count
                        FROM USER_CONSTRAINTS
                        WHERE TABLE_NAME = '{table}'
                          AND CONSTRAINT_NAME = 'FK_{table}_APPOINTMENTS_APPOINTMENT_ID';
                        IF v_count = 0 THEN
                            EXECUTE IMMEDIATE 'ALTER TABLE {table} ADD CONSTRAINT FK_{table}_APPOINTMENTS_APPOINTMENT_ID '
                                || 'FOREIGN KEY (APPOINTMENT_ID) REFERENCES APPOINTMENTS (APPOINTMENT_ID) ON DELETE CASCADE';
                        END IF;
                    END;
                    """);
            }

            // Una sola estancia activa por mascota, también ante admisiones simultáneas.
            // Índice funcional (EF no puede modelarlo): Oracle excluye los NULL de la unicidad,
            // así que las estancias dadas de alta no cuentan. Si ya existe un índice con la misma
            // expresión bajo otro nombre (la rama anterior lo creó como IX_HOSP_STAY_ACTIVE_PER_PET),
            // se renombra si es único o se reemplaza si no lo es: el mapper de conflictos
            // (OracleHospitalizationStayConflictMapper) depende del nombre UX_HOSP_STAY_ACTIVE_PER_PET.
            migrationBuilder.Sql(
                """
                DECLARE
                    v_count NUMBER;
                    v_expr VARCHAR2(4000);
                    v_existing VARCHAR2(128);
                    v_uniqueness VARCHAR2(9);
                BEGIN
                    SELECT COUNT(*) INTO v_count
                    FROM USER_INDEXES
                    WHERE INDEX_NAME = 'UX_HOSP_STAY_ACTIVE_PER_PET';

                    IF v_count = 0 THEN
                        FOR r IN (
                            SELECT e.INDEX_NAME, e.COLUMN_EXPRESSION, i.UNIQUENESS
                            FROM USER_IND_EXPRESSIONS e
                            JOIN USER_INDEXES i ON i.INDEX_NAME = e.INDEX_NAME
                            WHERE e.TABLE_NAME = 'HOSPITALIZATION_STAYS')
                        LOOP
                            -- Oracle puede guardar la expresión como CASE WHEN ESTADO=0 ... o
                            -- como CASE "ESTADO" WHEN 0 ... (así la dejó la rama anterior).
                            v_expr := r.COLUMN_EXPRESSION; -- LONG: primero a VARCHAR2
                            v_expr := UPPER(REPLACE(REPLACE(v_expr, '"', ''), ' ', ''));
                            IF v_expr IN (
                                'CASEWHENESTADO=0THENCLIENT_PET_IDEND',
                                'CASEESTADOWHEN0THENCLIENT_PET_IDEND') THEN
                                v_existing := r.INDEX_NAME;
                                v_uniqueness := r.UNIQUENESS;
                            END IF;
                        END LOOP;

                        IF v_existing IS NOT NULL AND v_uniqueness = 'UNIQUE' THEN
                            EXECUTE IMMEDIATE
                                'ALTER INDEX ' || v_existing || ' RENAME TO UX_HOSP_STAY_ACTIVE_PER_PET';
                        ELSE
                            IF v_existing IS NOT NULL THEN
                                EXECUTE IMMEDIATE 'DROP INDEX ' || v_existing;
                            END IF;
                            EXECUTE IMMEDIATE
                                'CREATE UNIQUE INDEX UX_HOSP_STAY_ACTIVE_PER_PET ON HOSPITALIZATION_STAYS '
                                || '(CASE WHEN ESTADO = 0 THEN CLIENT_PET_ID END)';
                        END IF;
                    END IF;

                    -- El modelo espera además el índice no único IX_HOSP_STAY_ACTIVE_PER_PET
                    -- (CLIENT_PET_ID, ESTADO) que acelera GetActiveByPetIdAsync; se repone si falta.
                    SELECT COUNT(*) INTO v_count
                    FROM USER_INDEXES
                    WHERE INDEX_NAME = 'IX_HOSP_STAY_ACTIVE_PER_PET';
                    IF v_count = 0 THEN
                        BEGIN
                            EXECUTE IMMEDIATE
                                'CREATE INDEX IX_HOSP_STAY_ACTIVE_PER_PET ON HOSPITALIZATION_STAYS (CLIENT_PET_ID, ESTADO)';
                        EXCEPTION
                            WHEN OTHERS THEN
                                -- ORA-01408: otro índice ya cubre esas columnas; sirve igual.
                                IF SQLCODE != -1408 THEN
                                    RAISE;
                                END IF;
                        END;
                    END IF;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE
                    v_count NUMBER;
                BEGIN
                    SELECT COUNT(*) INTO v_count
                    FROM USER_INDEXES
                    WHERE INDEX_NAME = 'UX_HOSP_STAY_ACTIVE_PER_PET';
                    IF v_count = 1 THEN
                        EXECUTE IMMEDIATE 'DROP INDEX UX_HOSP_STAY_ACTIVE_PER_PET';
                    END IF;
                END;
                """);

            foreach (var table in OrderTables)
            {
                // Falla a propósito si ya existen órdenes sin cita (de hospitalización).
                migrationBuilder.Sql(
                    $"""
                    DECLARE
                        v_count NUMBER;
                        v_nullable VARCHAR2(1);
                    BEGIN
                        SELECT COUNT(*) INTO v_count
                        FROM USER_CONSTRAINTS
                        WHERE TABLE_NAME = '{table}' AND CONSTRAINT_NAME = 'CK_{table}_ORIGIN';
                        IF v_count = 1 THEN
                            EXECUTE IMMEDIATE 'ALTER TABLE {table} DROP CONSTRAINT CK_{table}_ORIGIN';
                        END IF;

                        SELECT NULLABLE INTO v_nullable
                        FROM USER_TAB_COLUMNS
                        WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = 'APPOINTMENT_ID';
                        IF v_nullable = 'Y' THEN
                            EXECUTE IMMEDIATE 'ALTER TABLE {table} MODIFY (APPOINTMENT_ID NOT NULL)';
                        END IF;
                    END;
                    """);
            }
        }
    }
}
