-- =============================================================================
-- controlled_test_data_reset_postcheck.sql
--
-- Postcheck de SOLO LECTURA, en sesion independiente, despues del reset
-- controlado de datos de prueba. No ejecuta DML. Ver README.md en esta carpeta.
--
-- Uso (SQL*Plus, sesion ya conectada al esquema objetivo):
--   @controlled_test_data_reset_postcheck.sql <PDB_ESPERADA> <ESQUEMA_ESPERADO> <BASELINE_COUNTS>
--
-- <BASELINE_COUNTS> es el valor impreso por el precheck/reset en la linea
-- BASELINE_COUNTS=... (solo conteos, por ejemplo MOD12-PERM40-STA6-...).
--
-- Salida: solo conteos y metadatos de esquema; nunca filas, IDs, correos,
-- telefonos, hashes ni tokens.
-- Codigo de salida: 0 = RESET_SUCCESS, 4 = POSTCHECK_FAILED, otro = error SQL/OS.
--
-- Criterio final aprobado:
--   USERS = 1 (el SuperAdmin activo).
--   ROLES = 5 (SuperAdmin, Administrador, Veterinario, Recepcionista, Auxiliar).
--   ROLE_PERMISSIONS = permisos actuales de esos cinco roles (mismo conteo que
--   BASELINE_COUNTS); no se resiembran ni se modifican.
--   Rol Cliente (por ID y por nombre), usuarios Cliente y permisos Cliente = 0.
--   CLIENTS_PHONE_FIX_20260925 (tabla manual historica fuera de EF) = 0.
--
-- Inventario: 46 USER_TABLES (44 del modelo EF, __EFMigrationsHistory y
-- CLIENTS_PHONE_FIX_20260925). 35 tablas afectadas: 31 deben estar en 0 y
-- 4 se preservan parcialmente (AGENT_HUMANS, USERS, ROLE_PERMISSIONS, ROLES).
-- =============================================================================

WHENEVER OSERROR EXIT FAILURE ROLLBACK
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET ECHO OFF
SET VERIFY OFF
SET FEEDBACK ON
SET AUTOCOMMIT OFF
SET EXITCOMMIT OFF
SET DEFINE ON
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED FORMAT WRAPPED
SET LINESIZE 250
SET TRIMOUT ON
SET TRIMSPOOL ON
SET TIMING OFF

SET TERMOUT OFF
COLUMN 1 NEW_VALUE 1
COLUMN 2 NEW_VALUE 2
COLUMN 3 NEW_VALUE 3
SELECT NULL "1", NULL "2", NULL "3" FROM DUAL WHERE 1 = 0;
SET TERMOUT ON

VARIABLE b_exit NUMBER
EXEC :b_exit := 4

DECLARE
  v_param_pdb      VARCHAR2(4000) := '&1';
  v_param_schema   VARCHAR2(4000) := '&2';
  v_param_baseline VARCHAR2(4000) := '&3';

  c_module              CONSTANT VARCHAR2(48)  := 'HUELLITAS_TEST_DATA_RESET';
  c_sa_role_id          CONSTANT VARCHAR2(36)  := '99999999-9999-9999-9999-999999999999';
  c_admin_role_id       CONSTANT VARCHAR2(36)  := '11111111-1111-1111-1111-111111111111';
  c_vet_role_id         CONSTANT VARCHAR2(36)  := '44444444-4444-4444-4444-444444444444';
  c_recep_role_id       CONSTANT VARCHAR2(36)  := '55555555-5555-5555-5555-555555555555';
  c_aux_role_id         CONSTANT VARCHAR2(36)  := '66666666-6666-6666-6666-666666666666';
  c_client_role_id      CONSTANT VARCHAR2(36)  := '77777777-7777-7777-7777-777777777777';
  c_expected_migrations CONSTANT PLS_INTEGER   := 78;
  c_last_migration      CONSTANT VARCHAR2(150) := '20260928135138_AddHospitalizationSettings';
  c_aux_table           CONSTANT VARCHAR2(128) := 'CLIENTS_PHONE_FIX_20260925';
  c_expected_tables     CONSTANT PLS_INTEGER   := 46;
  c_expected_deletes    CONSTANT PLS_INTEGER   := 35;

  TYPE t_names   IS TABLE OF VARCHAR2(128);
  TYPE t_by_name IS TABLE OF NUMBER INDEX BY VARCHAR2(128);

  TYPE t_schema IS RECORD (
    user_tables        NUMBER,
    missing_tables     NUMBER,
    unexpected_tables  NUMBER,
    fk_total           NUMBER,
    fk_unexpected      NUMBER,
    fk_order_violation NUMBER,
    fk_pairs_found     NUMBER,
    constraints_bad    NUMBER,
    indexes_bad        NUMBER,
    invalid_objects    NUMBER);

  -- Debe coincidir exactamente con controlled_test_data_reset.sql. La primera es
  -- la tabla manual historica fuera del modelo EF.
  c_delete_order CONSTANT t_names := t_names(
    'CLIENTS_PHONE_FIX_20260925',
    'NOTIFICATIONS',
    'VACCINATIONS',
    'MEDICAL_RECORDS',
    'MEDICATION_ORDER_ITEMS',
    'PROCEDURE_ORDER_ITEMS',
    'MEDICATION_ORDERS',
    'PROCEDURE_ORDERS',
    'SUPPLY_CONSUMPTIONS',
    'HOSPITALIZATION_NOTES',
    'HOSPITALIZATION_STAYS',
    'APPOINTMENT_STATUS_HISTORIES',
    'APPOINTMENTS',
    'CHAT_MESSAGES',
    'CHAT_ESCALATIONS',
    'CHAT_PARTICIPANTS',
    'TELEGRAM_USER_LINKS',
    'CHAT_CONVERSATIONS',
    'TELEGRAM_INBOUND_UPDATES',
    'CONTACT_VERIFICATION_SESSIONS',
    'CLIENTS_PETS',
    'PETS',
    'CLIENTS',
    'VETERINARIAN_ABSENCES',
    'AVAILABILITIES',
    'VETERINARIANS',
    'AGENT_HUMANS',
    'USER_TOKENS',
    'USERS',
    'ROLE_PERMISSIONS',
    'ROLES',
    'SERVICES',
    'MEDICATIONS',
    'PROCEDURES',
    'SUPPLIES');

  c_partial CONSTANT t_names := t_names('AGENT_HUMANS', 'USERS', 'ROLE_PERMISSIONS', 'ROLES');

  c_structural CONSTANT t_names := t_names(
    'STATUS_APPOINTMENTS', 'SENDER_TYPES', 'ESCALATIONS_STATUSES', 'SPECIES',
    'RACES', 'SPECIALTIES', 'TYPE_SERVICES', 'DIAGNOSTICS');

  c_other_preserved CONSTANT t_names := t_names(
    '__EFMigrationsHistory', 'MODULES', 'HOSPITALIZATION_SETTINGS');

  c_fk_pairs CONSTANT t_names := t_names(
    'AGENT_HUMANS>USERS',
    'APPOINTMENT_STATUS_HISTORIES>APPOINTMENTS',
    'APPOINTMENT_STATUS_HISTORIES>CLIENTS_PETS',
    'APPOINTMENT_STATUS_HISTORIES>STATUS_APPOINTMENTS',
    'APPOINTMENTS>AVAILABILITIES',
    'APPOINTMENTS>CLIENTS_PETS',
    'APPOINTMENTS>SERVICES',
    'APPOINTMENTS>STATUS_APPOINTMENTS',
    'APPOINTMENTS>VETERINARIANS',
    'AVAILABILITIES>VETERINARIANS',
    'CHAT_ESCALATIONS>CHAT_CONVERSATIONS',
    'CHAT_ESCALATIONS>ESCALATIONS_STATUSES',
    'CHAT_MESSAGES>CHAT_CONVERSATIONS',
    'CHAT_MESSAGES>CHAT_PARTICIPANTS',
    'CHAT_MESSAGES>SENDER_TYPES',
    'CHAT_PARTICIPANTS>AGENT_HUMANS',
    'CHAT_PARTICIPANTS>CHAT_CONVERSATIONS',
    'CHAT_PARTICIPANTS>CLIENTS',
    'CHAT_PARTICIPANTS>SENDER_TYPES',
    'CLIENTS_PETS>CLIENTS',
    'CLIENTS_PETS>PETS',
    'HOSPITALIZATION_NOTES>HOSPITALIZATION_STAYS',
    'HOSPITALIZATION_STAYS>CLIENTS_PETS',
    'MEDICAL_RECORDS>APPOINTMENTS',
    'MEDICAL_RECORDS>CLIENTS_PETS',
    'MEDICAL_RECORDS>DIAGNOSTICS',
    'MEDICATION_ORDERS>APPOINTMENTS',
    'MEDICATION_ORDERS>CLIENTS_PETS',
    'MEDICATION_ORDERS>HOSPITALIZATION_STAYS',
    'MEDICATION_ORDERS>VETERINARIANS',
    'MEDICATION_ORDER_ITEMS>MEDICATIONS',
    'MEDICATION_ORDER_ITEMS>MEDICATION_ORDERS',
    'NOTIFICATIONS>APPOINTMENTS',
    'NOTIFICATIONS>CLIENTS',
    'NOTIFICATIONS>USERS',
    'PETS>RACES',
    'PETS>SPECIES',
    'PROCEDURE_ORDERS>APPOINTMENTS',
    'PROCEDURE_ORDERS>CLIENTS_PETS',
    'PROCEDURE_ORDERS>HOSPITALIZATION_STAYS',
    'PROCEDURE_ORDERS>VETERINARIANS',
    'PROCEDURE_ORDER_ITEMS>PROCEDURES',
    'PROCEDURE_ORDER_ITEMS>PROCEDURE_ORDERS',
    'RACES>SPECIES',
    'ROLE_PERMISSIONS>MODULES',
    'ROLE_PERMISSIONS>ROLES',
    'SERVICES>TYPE_SERVICES',
    'SUPPLY_CONSUMPTIONS>HOSPITALIZATION_STAYS',
    'SUPPLY_CONSUMPTIONS>SUPPLIES',
    'TELEGRAM_USER_LINKS>CLIENTS',
    'USER_TOKENS>USERS',
    'USERS>ROLES',
    'VACCINATIONS>CLIENTS_PETS',
    'VACCINATIONS>MEDICAL_RECORDS',
    'VETERINARIAN_ABSENCES>VETERINARIANS',
    'VETERINARIANS>SPECIALTIES',
    'VETERINARIANS>USERS');

  c_valid_role_ids   CONSTANT t_names := t_names(
    c_sa_role_id, c_admin_role_id, c_vet_role_id, c_recep_role_id, c_aux_role_id);
  c_valid_role_names CONSTANT t_names := t_names(
    'SuperAdmin', 'Administrador', 'Veterinario', 'Recepcionista', 'Auxiliar');

  v_pdb              VARCHAR2(128);
  v_schema           VARCHAR2(128);
  v_user             VARCHAR2(128);
  v_expected_tables  t_names;
  v_structure        t_by_name;
  v_schema_state     t_schema;
  v_fail             PLS_INTEGER := 0;
  v_sa_user_id       VARCHAR2(36);
  v_mig_count        NUMBER;
  v_mig_last         VARCHAR2(150);
  v_token            VARCHAR2(4000);
  v_n                NUMBER;
  v_m                NUMBER;

  PROCEDURE put(p_line VARCHAR2) IS
  BEGIN
    DBMS_OUTPUT.PUT_LINE(p_line);
  END;

  PROCEDURE fail(p_reason VARCHAR2) IS
  BEGIN
    v_fail := v_fail + 1;
    put('POSTCHECK_FAIL=' || p_reason);
  END;

  FUNCTION pos(p_name VARCHAR2, p_list t_names) RETURN PLS_INTEGER IS
  BEGIN
    FOR i IN 1 .. p_list.COUNT LOOP
      IF p_list(i) = p_name THEN
        RETURN i;
      END IF;
    END LOOP;
    RETURN 0;
  END;

  FUNCTION qn(p_name VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    RETURN DBMS_ASSERT.ENQUOTE_NAME(p_name, FALSE);
  END;

  FUNCTION count_rows(p_table VARCHAR2) RETURN NUMBER IS
    v NUMBER;
  BEGIN
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || qn(p_table) INTO v;
    RETURN v;
  END;

  FUNCTION table_present(p_table VARCHAR2) RETURN BOOLEAN IS
    v NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v FROM USER_TABLES WHERE TABLE_NAME = p_table;
    RETURN v = 1;
  END;

  PROCEDURE inspect_schema(o OUT t_schema) IS
    v_seen  t_by_name;
    v_pair  VARCHAR2(300);
    v_cnt   NUMBER;
  BEGIN
    o.user_tables := 0;
    o.missing_tables := 0;
    o.unexpected_tables := 0;
    o.fk_total := 0;
    o.fk_unexpected := 0;
    o.fk_order_violation := 0;
    o.fk_pairs_found := 0;

    FOR i IN 1 .. v_expected_tables.COUNT LOOP
      SELECT COUNT(*) INTO v_cnt FROM USER_TABLES WHERE TABLE_NAME = v_expected_tables(i);
      IF v_cnt <> 1 THEN
        o.missing_tables := o.missing_tables + 1;
        put('MISSING_TABLE=' || v_expected_tables(i));
      END IF;
    END LOOP;

    FOR r IN (SELECT TABLE_NAME
                FROM USER_TABLES
               WHERE DROPPED = 'NO'
                 AND NESTED = 'NO'
                 AND SECONDARY = 'N'
                 AND (IOT_TYPE IS NULL OR IOT_TYPE = 'IOT')
                 AND TABLE_NAME NOT LIKE 'BIN$%'
               ORDER BY TABLE_NAME) LOOP
      o.user_tables := o.user_tables + 1;
      IF pos(r.TABLE_NAME, v_expected_tables) = 0 THEN
        o.unexpected_tables := o.unexpected_tables + 1;
        put('UNEXPECTED_TABLE=' || r.TABLE_NAME);
      END IF;
    END LOOP;

    FOR r IN (SELECT c.CONSTRAINT_NAME,
                     c.TABLE_NAME AS CHILD_TABLE,
                     p.TABLE_NAME AS PARENT_TABLE,
                     c.STATUS,
                     c.VALIDATED
                FROM USER_CONSTRAINTS c
                LEFT JOIN USER_CONSTRAINTS p
                  ON p.CONSTRAINT_NAME = c.R_CONSTRAINT_NAME
                 AND p.OWNER = c.R_OWNER
               WHERE c.CONSTRAINT_TYPE = 'R'
                 AND c.TABLE_NAME NOT LIKE 'BIN$%'
               ORDER BY c.CONSTRAINT_NAME) LOOP
      o.fk_total := o.fk_total + 1;
      v_pair := r.CHILD_TABLE || '>' || NVL(r.PARENT_TABLE, '?');
      IF pos(v_pair, c_fk_pairs) = 0 THEN
        o.fk_unexpected := o.fk_unexpected + 1;
        put('UNEXPECTED_FK=' || r.CONSTRAINT_NAME);
      ELSE
        v_seen(v_pair) := 1;
      END IF;
      IF r.PARENT_TABLE IS NOT NULL AND pos(r.PARENT_TABLE, c_delete_order) > 0 THEN
        IF pos(r.CHILD_TABLE, c_delete_order) = 0
           OR pos(r.CHILD_TABLE, c_delete_order) >= pos(r.PARENT_TABLE, c_delete_order) THEN
          o.fk_order_violation := o.fk_order_violation + 1;
          put('FK_ORDER_VIOLATION=' || r.CONSTRAINT_NAME);
        END IF;
      END IF;
      put('FK[' || v_pair || ']=' || r.STATUS || '/' || r.VALIDATED);
    END LOOP;
    o.fk_pairs_found := v_seen.COUNT;

    FOR i IN 1 .. c_fk_pairs.COUNT LOOP
      IF NOT v_seen.EXISTS(c_fk_pairs(i)) THEN
        put('MISSING_FK_PAIR=' || c_fk_pairs(i));
      END IF;
    END LOOP;

    SELECT COUNT(*) INTO o.constraints_bad
      FROM USER_CONSTRAINTS
     WHERE TABLE_NAME NOT LIKE 'BIN$%'
       AND (STATUS <> 'ENABLED' OR VALIDATED <> 'VALIDATED');
    SELECT COUNT(*) INTO o.indexes_bad
      FROM USER_INDEXES
     WHERE TABLE_NAME NOT LIKE 'BIN$%'
       AND STATUS NOT IN ('VALID', 'N/A');
    SELECT COUNT(*) INTO o.invalid_objects
      FROM USER_OBJECTS
     WHERE STATUS <> 'VALID'
       AND OBJECT_NAME NOT LIKE 'BIN$%';

    put('SCHEMA_USER_TABLES=' || o.user_tables || ' EXPECTED=' || c_expected_tables);
    put('SCHEMA_MISSING_TABLES=' || o.missing_tables);
    put('SCHEMA_UNEXPECTED_TABLES=' || o.unexpected_tables);
    put('FK_TOTAL=' || o.fk_total);
    put('FK_EXPECTED_PAIRS_FOUND=' || o.fk_pairs_found || '/' || c_fk_pairs.COUNT);
    put('FK_UNEXPECTED=' || o.fk_unexpected);
    put('FK_ORDER_VIOLATIONS=' || o.fk_order_violation);
    put('CONSTRAINTS_NOT_ENABLED_VALIDATED=' || o.constraints_bad);
    put('INDEXES_NOT_VALID=' || o.indexes_bad);
    put('INVALID_OBJECTS=' || o.invalid_objects);
  END;

  PROCEDURE snapshot_structure(o OUT t_by_name) IS
    v_cnt NUMBER;
  BEGIN
    FOR i IN 1 .. c_structural.COUNT LOOP
      o('CAT:' || c_structural(i)) := count_rows(c_structural(i));
    END LOOP;

    SELECT COUNT(*) INTO v_cnt
      FROM SENDER_TYPES
     WHERE SENDER_TYPES_ID IN (
       '82000000-0000-0000-0000-000000000001',
       '82000000-0000-0000-0000-000000000002',
       '82000000-0000-0000-0000-000000000003',
       '82000000-0000-0000-0000-000000000004');
    o('FIX:SENDER_TYPES') := v_cnt;

    SELECT COUNT(*) INTO v_cnt
      FROM ESCALATIONS_STATUSES
     WHERE ESCALATIONS_ID IN (
       '85000000-0000-0000-0000-000000000001',
       '85000000-0000-0000-0000-000000000002',
       '85000000-0000-0000-0000-000000000003',
       '85000000-0000-0000-0000-000000000004',
       '85000000-0000-0000-0000-000000000005');
    o('FIX:ESCALATIONS_STATUSES') := v_cnt;

    SELECT COUNT(*) INTO v_cnt
      FROM STATUS_APPOINTMENTS
     WHERE UPPER(NAME) IN ('AGENDADA', 'ATENDIDA', 'CONFIRMADA', 'EN_PROGRESO', 'CANCELADA', 'NO_ASISTIO');
    o('FIX:STATUS_APPOINTMENTS') := v_cnt;

    SELECT COUNT(*) INTO v_cnt FROM MODULES;
    o('MODULES') := v_cnt;

    SELECT COUNT(*) INTO v_cnt
      FROM ROLE_PERMISSIONS
     WHERE ROLE_ID IN (
       '99999999-9999-9999-9999-999999999999',
       '11111111-1111-1111-1111-111111111111',
       '44444444-4444-4444-4444-444444444444',
       '55555555-5555-5555-5555-555555555555',
       '66666666-6666-6666-6666-666666666666');
    o('PERMISSIONS_VALID') := v_cnt;
  END;

  FUNCTION baseline_token(p t_by_name) RETURN VARCHAR2 IS
  BEGIN
    RETURN 'MOD' || p('MODULES')
        || '-PERM' || p('PERMISSIONS_VALID')
        || '-STA' || p('CAT:STATUS_APPOINTMENTS')
        || '-SND' || p('CAT:SENDER_TYPES')
        || '-ESC' || p('CAT:ESCALATIONS_STATUSES')
        || '-SPE' || p('CAT:SPECIES')
        || '-RAC' || p('CAT:RACES')
        || '-SPC' || p('CAT:SPECIALTIES')
        || '-TYS' || p('CAT:TYPE_SERVICES')
        || '-DIA' || p('CAT:DIAGNOSTICS');
  END;

BEGIN
  DBMS_APPLICATION_INFO.SET_MODULE(module_name => c_module, action_name => 'POSTCHECK');

  put('POSTCHECK_TIME_UTC='
      || TO_CHAR(SYSTIMESTAMP AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.FF6"Z"'));

  v_pdb    := SYS_CONTEXT('USERENV', 'CON_NAME');
  v_schema := SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');
  v_user   := SYS_CONTEXT('USERENV', 'SESSION_USER');
  put('CURRENT_PDB=' || v_pdb);
  put('CURRENT_SCHEMA=' || v_schema);
  put('ORACLE_VERSION=' || DBMS_DB_VERSION.VERSION || '.' || DBMS_DB_VERSION.RELEASE);

  -- Parametros y entorno.
  IF v_param_pdb IS NULL OR v_param_schema IS NULL OR v_param_baseline IS NULL THEN
    fail('PARAMETERS_MISSING');
  ELSIF NOT REGEXP_LIKE(v_param_pdb, '^[A-Za-z][A-Za-z0-9_$]{0,127}$')
     OR NOT REGEXP_LIKE(v_param_schema, '^[A-Za-z][A-Za-z0-9_$]{0,127}$')
     OR NOT REGEXP_LIKE(v_param_baseline, '^[A-Z0-9-]{1,200}$') THEN
    fail('PARAMETER_FORMAT');
  ELSE
    IF v_pdb IS NULL OR v_pdb <> UPPER(v_param_pdb) THEN
      fail('PDB_MISMATCH');
    END IF;
    IF v_schema IS NULL OR v_schema <> UPPER(v_param_schema) THEN
      fail('SCHEMA_MISMATCH');
    END IF;
    IF v_user IS NULL OR v_user <> UPPER(v_param_schema) THEN
      fail('SESSION_USER_MISMATCH');
    END IF;
  END IF;

  v_expected_tables := c_delete_order MULTISET UNION ALL c_structural MULTISET UNION ALL c_other_preserved;
  put('INVENTORY_EXPECTED_TABLES=' || v_expected_tables.COUNT
      || ' DELETE_TABLES=' || c_delete_order.COUNT
      || ' ZERO_TABLES=' || (c_delete_order.COUNT - c_partial.COUNT)
      || ' PARTIAL_TABLES=' || c_partial.COUNT);
  IF v_expected_tables.COUNT <> c_expected_tables OR c_delete_order.COUNT <> c_expected_deletes THEN
    fail('SCRIPT_INVENTORY_INCONSISTENT');
  END IF;

  -- Migraciones.
  SELECT COUNT(*), MAX("MigrationId") INTO v_mig_count, v_mig_last FROM "__EFMigrationsHistory";
  put('MIGRATIONS_COUNT=' || v_mig_count || ' EXPECTED=' || c_expected_migrations);
  put('MIGRATIONS_LAST=' || NVL(v_mig_last, '-') || ' EXPECTED=' || c_last_migration);
  IF v_mig_count <> c_expected_migrations OR NVL(v_mig_last, '-') <> c_last_migration THEN
    fail('MIGRATIONS_MISMATCH');
  END IF;

  -- Roles validos, rol Cliente ausente.
  FOR i IN 1 .. c_valid_role_ids.COUNT LOOP
    SELECT COUNT(*) INTO v_n FROM ROLES WHERE ROLE_ID = c_valid_role_ids(i) AND NAME = c_valid_role_names(i);
    SELECT COUNT(*) INTO v_m FROM ROLE_PERMISSIONS WHERE ROLE_ID = c_valid_role_ids(i);
    put('ROLE[' || c_valid_role_names(i) || ']=' || v_n || ' PERMISSIONS=' || v_m);
    IF v_n <> 1 THEN
      fail('ROLE_MISSING_OR_RENAMED:' || c_valid_role_names(i));
    END IF;
  END LOOP;

  SELECT COUNT(*) INTO v_n FROM ROLES;
  put('ROLES_TOTAL=' || v_n || ' EXPECTED=5');
  IF v_n <> 5 THEN
    fail('ROLES_TOTAL_NOT_5');
  END IF;

  SELECT COUNT(*) INTO v_n
    FROM ROLES
   WHERE ROLE_ID NOT IN (
     '99999999-9999-9999-9999-999999999999',
     '11111111-1111-1111-1111-111111111111',
     '44444444-4444-4444-4444-444444444444',
     '55555555-5555-5555-5555-555555555555',
     '66666666-6666-6666-6666-666666666666');
  put('ROLES_UNEXPECTED=' || v_n);
  IF v_n <> 0 THEN
    fail('ROLES_NOT_EXACTLY_APPROVED');
  END IF;

  SELECT COUNT(*) INTO v_n FROM ROLES WHERE ROLE_ID = c_client_role_id;
  put('CLIENT_ROLE=' || v_n);
  IF v_n <> 0 THEN
    fail('CLIENT_ROLE_PRESENT');
  END IF;
  SELECT COUNT(*) INTO v_n FROM ROLES WHERE UPPER(TRIM(NAME)) = 'CLIENTE';
  put('ROLES_NAMED_CLIENTE=' || v_n);
  IF v_n <> 0 THEN
    fail('CLIENT_ROLE_NAME_PRESENT');
  END IF;
  SELECT COUNT(*) INTO v_n FROM USERS WHERE ROLE_ID = c_client_role_id;
  put('CLIENT_USERS=' || v_n);
  IF v_n <> 0 THEN
    fail('CLIENT_USERS_PRESENT');
  END IF;
  SELECT COUNT(*) INTO v_n FROM ROLE_PERMISSIONS WHERE ROLE_ID = c_client_role_id;
  put('CLIENT_PERMISSIONS=' || v_n);
  IF v_n <> 0 THEN
    fail('CLIENT_PERMISSIONS_PRESENT');
  END IF;

  -- Usuarios: solo el SuperAdmin, activo y con hash.
  SELECT COUNT(*) INTO v_n FROM USERS;
  put('USERS_TOTAL=' || v_n || ' EXPECTED=1');
  IF v_n <> 1 THEN
    fail('USERS_TOTAL_NOT_1');
  END IF;
  SELECT COUNT(*) INTO v_n
    FROM USERS
   WHERE ROLE_ID = c_sa_role_id
     AND IS_ACTIVE = 1
     AND PASSWORD_HASH IS NOT NULL
     AND LENGTH(TRIM(PASSWORD_HASH)) > 0;
  put('SUPERADMIN_VALID=' || v_n || ' EXPECTED=1');
  IF v_n <> 1 THEN
    fail('SUPERADMIN_NOT_EXACTLY_ONE_VALID');
  ELSE
    SELECT USER_ID INTO v_sa_user_id FROM USERS WHERE ROLE_ID = c_sa_role_id;
  END IF;

  -- AGENT_HUMANS: 0 filas, o 1 fila perteneciente al SuperAdmin.
  SELECT COUNT(*) INTO v_n FROM AGENT_HUMANS;
  v_m := 0;
  IF v_sa_user_id IS NOT NULL THEN
    SELECT COUNT(*) INTO v_m FROM AGENT_HUMANS WHERE USER_ID = v_sa_user_id;
  END IF;
  put('AGENT_HUMANS_TOTAL=' || v_n || ' LINKED_TO_SUPERADMIN=' || v_m);
  IF v_n > 1 OR v_n <> v_m THEN
    fail('AGENT_HUMANS_NOT_ONLY_SUPERADMIN');
  END IF;

  -- Tabla manual historica: obligatoria y vacia.
  IF NOT table_present(c_aux_table) THEN
    put('AUX_TABLE_PRESENT=0');
    fail('AUX_TABLE_MISSING');
  ELSE
    v_n := count_rows(c_aux_table);
    put('AUX_TABLE_PRESENT=1 ROWS=' || v_n || ' EXPECTED=0');
    IF v_n <> 0 THEN
      fail('AUX_TABLE_NOT_EMPTY');
    END IF;
  END IF;

  -- Tablas que deben terminar en 0 (todas las afectadas salvo las parciales).
  v_m := 0;
  FOR i IN 1 .. c_delete_order.COUNT LOOP
    IF pos(c_delete_order(i), c_partial) = 0 THEN
      IF NOT table_present(c_delete_order(i)) THEN
        put('TABLE_COUNT[' || c_delete_order(i) || ']=MISSING');
        fail('TABLE_MISSING:' || c_delete_order(i));
      ELSE
        v_n := count_rows(c_delete_order(i));
        put('TABLE_COUNT[' || c_delete_order(i) || ']=' || v_n);
        IF v_n = 0 THEN
          v_m := v_m + 1;
        ELSE
          fail('TABLE_NOT_EMPTY:' || c_delete_order(i));
        END IF;
      END IF;
    END IF;
  END LOOP;
  put('ZERO_TABLES_OK=' || v_m || '/' || (c_delete_order.COUNT - c_partial.COUNT));

  SELECT COUNT(*) INTO v_n FROM HOSPITALIZATION_SETTINGS;
  put('TABLE_COUNT[HOSPITALIZATION_SETTINGS]=' || v_n || ' EXPECTED=0');
  IF v_n <> 0 THEN
    fail('HOSPITALIZATION_SETTINGS_NOT_EMPTY');
  END IF;

  -- Catalogos, modulos y permisos: mismos conteos que el precheck.
  snapshot_structure(v_structure);
  DECLARE
    k VARCHAR2(128);
  BEGIN
    k := v_structure.FIRST;
    WHILE k IS NOT NULL LOOP
      put('STRUCTURE[' || k || ']=' || v_structure(k));
      k := v_structure.NEXT(k);
    END LOOP;
  END;
  FOR i IN 1 .. c_structural.COUNT LOOP
    IF v_structure('CAT:' || c_structural(i)) = 0 THEN
      fail('EMPTY_CATALOG:' || c_structural(i));
    END IF;
  END LOOP;
  IF v_structure('FIX:SENDER_TYPES') <> 4 THEN
    fail('FIXED_IDS:SENDER_TYPES');
  END IF;
  IF v_structure('FIX:ESCALATIONS_STATUSES') <> 5 THEN
    fail('FIXED_IDS:ESCALATIONS_STATUSES');
  END IF;
  IF v_structure('FIX:STATUS_APPOINTMENTS') <> 6 THEN
    fail('FIXED_NAMES:STATUS_APPOINTMENTS');
  END IF;
  SELECT COUNT(*) INTO v_n FROM ROLE_PERMISSIONS;
  put('ROLE_PERMISSIONS_TOTAL=' || v_n || ' APPROVED_ROLES=' || v_structure('PERMISSIONS_VALID'));
  IF v_n <> v_structure('PERMISSIONS_VALID') THEN
    fail('ROLE_PERMISSIONS_NOT_ONLY_APPROVED_ROLES');
  END IF;
  v_token := baseline_token(v_structure);
  put('BASELINE_COUNTS=' || v_token);
  IF v_param_baseline IS NULL OR v_token <> v_param_baseline THEN
    fail('BASELINE_COUNTS_MISMATCH');
  END IF;

  -- Esquema intacto.
  inspect_schema(v_schema_state);
  IF v_schema_state.missing_tables > 0 THEN
    fail('MISSING_TABLES');
  END IF;
  IF v_schema_state.unexpected_tables > 0 THEN
    fail('UNEXPECTED_TABLES');
  END IF;
  IF v_schema_state.user_tables <> c_expected_tables THEN
    fail('USER_TABLES_NOT_46');
  END IF;
  IF v_schema_state.fk_unexpected > 0
     OR v_schema_state.fk_order_violation > 0
     OR v_schema_state.fk_pairs_found <> c_fk_pairs.COUNT THEN
    fail('FK_INVENTORY_MISMATCH');
  END IF;
  IF v_schema_state.constraints_bad > 0 OR v_schema_state.indexes_bad > 0 THEN
    fail('CONSTRAINTS_OR_INDEXES_NOT_VALID');
  END IF;
  IF v_schema_state.invalid_objects > 0 THEN
    fail('INVALID_OBJECTS');
  END IF;

  put('POSTCHECK_FAILURES=' || v_fail);
  IF v_fail = 0 THEN
    put('POSTCHECK_RESULT=RESET_SUCCESS');
    :b_exit := 0;
  ELSE
    put('POSTCHECK_RESULT=POSTCHECK_FAILED');
    :b_exit := 4;
  END IF;
  DBMS_APPLICATION_INFO.SET_MODULE(NULL, NULL);
END;
/

EXIT :b_exit ROLLBACK
