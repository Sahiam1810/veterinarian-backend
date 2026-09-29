-- =============================================================================
-- controlled_test_data_reset_precheck.sql
--
-- Precheck de SOLO LECTURA para el reset controlado de datos de prueba.
-- No ejecuta DML. Ver README.md en esta carpeta.
--
-- Uso (SQL*Plus, sesion ya conectada al esquema objetivo):
--   @controlled_test_data_reset_precheck.sql <PDB_ESPERADA> <ESQUEMA_ESPERADO>
--
-- Salida: solo conteos y metadatos de esquema; nunca filas, IDs, correos,
-- telefonos, hashes ni tokens.
-- Codigo de salida: 0 = READY, 3 = NOT_READY, otro = error de SQL/OS.
--
-- Estado final aprobado que se planifica (PLANNED_KEEP):
--   USERS = 1 (el SuperAdmin activo).
--   ROLES = 5 (SuperAdmin, Administrador, Veterinario, Recepcionista, Auxiliar).
--   ROLE_PERMISSIONS = permisos actuales de esos cinco roles, sin cambios.
--   Rol Cliente, usuarios Cliente y permisos Cliente = 0.
--
-- Inventario: 46 USER_TABLES (44 del modelo EF, __EFMigrationsHistory y la
-- tabla manual historica CLIENTS_PHONE_FIX_20260925, fuera del modelo EF).
-- 35 tablas afectadas: 31 terminan en 0 y 4 se preservan parcialmente
-- (AGENT_HUMANS, USERS, ROLE_PERMISSIONS, ROLES).
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
SELECT NULL "1", NULL "2" FROM DUAL WHERE 1 = 0;
SET TERMOUT ON

VARIABLE b_exit NUMBER
EXEC :b_exit := 3

DECLARE
  v_param_pdb     VARCHAR2(4000) := '&1';
  v_param_schema  VARCHAR2(4000) := '&2';

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
    invalid_objects    NUMBER,
    total_objects      NUMBER,
    total_constraints  NUMBER,
    total_indexes      NUMBER);

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
  c_structural_pk CONSTANT t_names := t_names(
    'STATUS_APPOINTMENT_ID', 'SENDER_TYPES_ID', 'ESCALATIONS_ID', 'SPECIES_ID',
    'RACE_ID', 'SPECIALTY_ID', 'TYPE_SERVICE_ID', 'ID');

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

  v_cutoff_tz        TIMESTAMP WITH TIME ZONE;
  v_cutoff_utc       TIMESTAMP;
  v_pdb              VARCHAR2(128);
  v_schema           VARCHAR2(128);
  v_user             VARCHAR2(128);
  v_expected_tables  t_names;
  v_caps             t_by_name;
  v_counts           t_by_name;
  v_structure        t_by_name;
  v_schema_state     t_schema;
  v_reasons          PLS_INTEGER := 0;
  v_sa_user_id       VARCHAR2(36);
  v_sa_agents        NUMBER := 0;
  v_keep_sa_agent    NUMBER := 0;
  v_mig_count        NUMBER;
  v_mig_last         VARCHAR2(150);
  v_n                NUMBER;
  v_total            NUMBER;
  v_planned          NUMBER;
  v_aux_present      NUMBER := 0;
  v_aux_ok           BOOLEAN := FALSE;
  v_aux_type         VARCHAR2(128);

  PROCEDURE put(p_line VARCHAR2) IS
  BEGIN
    DBMS_OUTPUT.PUT_LINE(p_line);
  END;

  PROCEDURE add_reason(p_reason VARCHAR2) IS
  BEGIN
    v_reasons := v_reasons + 1;
    put('NOT_READY_REASON=' || p_reason);
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

  -- La tabla manual historica no tiene CREATED_AT; su marca temporal es FIXED_AT.
  FUNCTION cutoff_expr(p_table VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    IF p_table = c_aux_table THEN
      RETURN 'SYS_EXTRACT_UTC(FIXED_AT)';
    END IF;
    RETURN 'CREATED_AT';
  END;

  FUNCTION count_created_after(p_table VARCHAR2, p_cutoff TIMESTAMP) RETURN NUMBER IS
    v NUMBER;
  BEGIN
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || qn(p_table) || ' WHERE ' || cutoff_expr(p_table) || ' > :cutoff'
      INTO v USING p_cutoff;
    RETURN v;
  END;

  FUNCTION table_present(p_table VARCHAR2) RETURN BOOLEAN IS
    v NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v FROM USER_TABLES WHERE TABLE_NAME = p_table;
    RETURN v = 1;
  END;

  FUNCTION fingerprint(p_table VARCHAR2, p_pk VARCHAR2) RETURN NUMBER IS
    v NUMBER;
  BEGIN
    EXECUTE IMMEDIATE 'SELECT NVL(SUM(ORA_HASH(' || qn(p_pk) || ')), 0) FROM ' || qn(p_table) INTO v;
    RETURN v;
  END;

  -- Topes de volumen por tabla (PENDIENTES DE APROBACION de la lider).
  -- Deben coincidir con controlled_test_data_reset.sql.
  PROCEDURE init_caps IS
  BEGIN
    -- Tope inicial de la tabla manual historica; se recaptura en la ventana.
    v_caps('CLIENTS_PHONE_FIX_20260925') := 6;
    v_caps('NOTIFICATIONS')                 := 50000;
    v_caps('VACCINATIONS')                  := 20000;
    v_caps('MEDICAL_RECORDS')               := 20000;
    v_caps('MEDICATION_ORDER_ITEMS')        := 50000;
    v_caps('PROCEDURE_ORDER_ITEMS')         := 50000;
    v_caps('MEDICATION_ORDERS')             := 20000;
    v_caps('PROCEDURE_ORDERS')              := 20000;
    v_caps('SUPPLY_CONSUMPTIONS')           := 50000;
    v_caps('HOSPITALIZATION_NOTES')         := 50000;
    v_caps('HOSPITALIZATION_STAYS')         := 10000;
    v_caps('APPOINTMENT_STATUS_HISTORIES')  := 100000;
    v_caps('APPOINTMENTS')                  := 50000;
    v_caps('CHAT_MESSAGES')                 := 200000;
    v_caps('CHAT_ESCALATIONS')              := 20000;
    v_caps('CHAT_PARTICIPANTS')             := 50000;
    v_caps('TELEGRAM_USER_LINKS')           := 10000;
    v_caps('CHAT_CONVERSATIONS')            := 20000;
    v_caps('TELEGRAM_INBOUND_UPDATES')      := 200000;
    v_caps('CONTACT_VERIFICATION_SESSIONS') := 50000;
    v_caps('CLIENTS_PETS')                  := 20000;
    v_caps('PETS')                          := 20000;
    v_caps('CLIENTS')                       := 20000;
    v_caps('VETERINARIAN_ABSENCES')         := 10000;
    v_caps('AVAILABILITIES')                := 20000;
    v_caps('VETERINARIANS')                 := 500;
    v_caps('AGENT_HUMANS')                  := 500;
    v_caps('USER_TOKENS')                   := 100000;
    v_caps('USERS')                         := 1000;
    v_caps('ROLE_PERMISSIONS')              := 2000;
    v_caps('ROLES')                         := 20;
    v_caps('SERVICES')                      := 2000;
    v_caps('MEDICATIONS')                   := 5000;
    v_caps('PROCEDURES')                    := 5000;
    v_caps('SUPPLIES')                      := 5000;
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
    SELECT COUNT(*) INTO o.total_objects
      FROM USER_OBJECTS WHERE OBJECT_NAME NOT LIKE 'BIN$%';
    SELECT COUNT(*) INTO o.total_constraints
      FROM USER_CONSTRAINTS WHERE TABLE_NAME NOT LIKE 'BIN$%';
    SELECT COUNT(*) INTO o.total_indexes
      FROM USER_INDEXES WHERE TABLE_NAME NOT LIKE 'BIN$%';

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
    put('SCHEMA_OBJECTS_TOTAL=' || o.total_objects);
    put('SCHEMA_CONSTRAINTS_TOTAL=' || o.total_constraints);
    put('SCHEMA_INDEXES_TOTAL=' || o.total_indexes);
  END;

  FUNCTION check_roles RETURN PLS_INTEGER IS
    v_failures PLS_INTEGER := 0;
    v_id       VARCHAR2(36);
    v_name     VARCHAR2(50);
    v_role     NUMBER;
    v_users    NUMBER;
    v_perms    NUMBER;
  BEGIN
    FOR i IN 1 .. c_valid_role_ids.COUNT LOOP
      v_id := c_valid_role_ids(i);
      v_name := c_valid_role_names(i);
      SELECT COUNT(*) INTO v_role  FROM ROLES WHERE ROLE_ID = v_id AND NAME = v_name;
      SELECT COUNT(*) INTO v_users FROM USERS WHERE ROLE_ID = v_id;
      SELECT COUNT(*) INTO v_perms FROM ROLE_PERMISSIONS WHERE ROLE_ID = v_id;
      put('ROLE[' || v_name || ']=' || v_role || ' USERS=' || v_users || ' PERMISSIONS=' || v_perms);
      IF v_role <> 1 THEN
        v_failures := v_failures + 1;
        put('ROLE_CHECK_FAIL=MISSING_OR_RENAMED:' || v_name);
      END IF;
    END LOOP;

    SELECT COUNT(*) INTO v_role  FROM ROLES WHERE ROLE_ID = c_client_role_id;
    SELECT COUNT(*) INTO v_users FROM USERS WHERE ROLE_ID = c_client_role_id;
    SELECT COUNT(*) INTO v_perms FROM ROLE_PERMISSIONS WHERE ROLE_ID = c_client_role_id;
    put('ROLE[Cliente]=' || v_role || ' USERS=' || v_users || ' PERMISSIONS=' || v_perms);
    IF v_role = 1 THEN
      SELECT COUNT(*) INTO v_role FROM ROLES WHERE ROLE_ID = c_client_role_id AND NAME = 'Cliente';
      IF v_role <> 1 THEN
        v_failures := v_failures + 1;
        put('ROLE_CHECK_FAIL=CLIENT_ROLE_NAME_MISMATCH');
      END IF;
    END IF;

    SELECT COUNT(*) INTO v_role FROM ROLES WHERE UPPER(TRIM(NAME)) = 'CLIENTE';
    put('ROLES_NAMED_CLIENTE=' || v_role);
    SELECT COUNT(*) INTO v_role
      FROM ROLES
     WHERE UPPER(TRIM(NAME)) = 'CLIENTE'
       AND ROLE_ID <> c_client_role_id;
    IF v_role > 0 THEN
      v_failures := v_failures + 1;
      put('ROLE_CHECK_FAIL=CLIENT_NAME_ON_UNEXPECTED_ROLE_ID');
    END IF;

    SELECT COUNT(*) INTO v_role
      FROM ROLES
     WHERE ROLE_ID NOT IN (
       '99999999-9999-9999-9999-999999999999',
       '11111111-1111-1111-1111-111111111111',
       '44444444-4444-4444-4444-444444444444',
       '55555555-5555-5555-5555-555555555555',
       '66666666-6666-6666-6666-666666666666',
       '77777777-7777-7777-7777-777777777777');
    put('ROLES_UNEXPECTED=' || v_role);
    IF v_role > 0 THEN
      v_failures := v_failures + 1;
      put('ROLE_CHECK_FAIL=UNEXPECTED_ROLES');
    END IF;

    SELECT COUNT(*) INTO v_role FROM ROLES;
    put('ROLES_TOTAL=' || v_role);
    RETURN v_failures;
  END;

  FUNCTION check_superadmin(o_user_id OUT VARCHAR2) RETURN PLS_INTEGER IS
    v_all   NUMBER;
    v_valid NUMBER;
  BEGIN
    o_user_id := NULL;
    SELECT COUNT(*) INTO v_all FROM USERS WHERE ROLE_ID = c_sa_role_id;
    SELECT COUNT(*) INTO v_valid
      FROM USERS
     WHERE ROLE_ID = c_sa_role_id
       AND IS_ACTIVE = 1
       AND PASSWORD_HASH IS NOT NULL
       AND LENGTH(TRIM(PASSWORD_HASH)) > 0;
    put('SUPERADMIN_USERS=' || v_all);
    put('SUPERADMIN_VALID=' || v_valid);
    IF v_all = 1 AND v_valid = 1 THEN
      SELECT USER_ID INTO o_user_id FROM USERS WHERE ROLE_ID = c_sa_role_id;
      RETURN 0;
    END IF;
    RETURN 1;
  END;

  PROCEDURE snapshot_structure(o OUT t_by_name) IS
    v_cnt NUMBER;
    v_fp  NUMBER;
  BEGIN
    FOR i IN 1 .. c_structural.COUNT LOOP
      o('CAT:' || c_structural(i)) := count_rows(c_structural(i));
      o('FP:' || c_structural(i)) := fingerprint(c_structural(i), c_structural_pk(i));
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

    SELECT COUNT(*), NVL(SUM(ORA_HASH(MODULE_ID || '|' || NAME)), 0)
      INTO v_cnt, v_fp
      FROM MODULES;
    o('MODULES') := v_cnt;
    o('FP:MODULES') := v_fp;

    SELECT COUNT(*),
           NVL(SUM(ORA_HASH(ROLE_PERMISSION_ID || '|' || ROLE_ID || '|' || MODULE_ID || '|'
                            || CAN_VIEW || '|' || CAN_CREATE || '|' || CAN_EDIT || '|' || CAN_DELETE)), 0)
      INTO v_cnt, v_fp
      FROM ROLE_PERMISSIONS
     WHERE ROLE_ID IN (
       '99999999-9999-9999-9999-999999999999',
       '11111111-1111-1111-1111-111111111111',
       '44444444-4444-4444-4444-444444444444',
       '55555555-5555-5555-5555-555555555555',
       '66666666-6666-6666-6666-666666666666');
    o('PERMISSIONS_VALID') := v_cnt;
    o('FP:PERMISSIONS_VALID') := v_fp;
  END;

  PROCEDURE print_structure(p t_by_name) IS
    k VARCHAR2(128);
  BEGIN
    k := p.FIRST;
    WHILE k IS NOT NULL LOOP
      IF SUBSTR(k, 1, 3) <> 'FP:' THEN
        put('STRUCTURE[' || k || ']=' || p(k));
      END IF;
      k := p.NEXT(k);
    END LOOP;
  END;

  FUNCTION structure_failures(p t_by_name) RETURN PLS_INTEGER IS
    v_failures PLS_INTEGER := 0;
  BEGIN
    FOR i IN 1 .. c_structural.COUNT LOOP
      IF p('CAT:' || c_structural(i)) = 0 THEN
        v_failures := v_failures + 1;
        put('STRUCTURE_CHECK_FAIL=EMPTY_CATALOG:' || c_structural(i));
      END IF;
    END LOOP;
    IF p('FIX:SENDER_TYPES') <> 4 THEN
      v_failures := v_failures + 1;
      put('STRUCTURE_CHECK_FAIL=FIXED_IDS:SENDER_TYPES');
    END IF;
    IF p('FIX:ESCALATIONS_STATUSES') <> 5 THEN
      v_failures := v_failures + 1;
      put('STRUCTURE_CHECK_FAIL=FIXED_IDS:ESCALATIONS_STATUSES');
    END IF;
    IF p('FIX:STATUS_APPOINTMENTS') <> 6 THEN
      v_failures := v_failures + 1;
      put('STRUCTURE_CHECK_FAIL=FIXED_NAMES:STATUS_APPOINTMENTS');
    END IF;
    IF p('MODULES') = 0 THEN
      v_failures := v_failures + 1;
      put('STRUCTURE_CHECK_FAIL=EMPTY_MODULES');
    END IF;
    IF p('PERMISSIONS_VALID') = 0 THEN
      v_failures := v_failures + 1;
      put('STRUCTURE_CHECK_FAIL=EMPTY_VALID_PERMISSIONS');
    END IF;
    RETURN v_failures;
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
  DBMS_APPLICATION_INFO.SET_MODULE(module_name => c_module, action_name => 'PRECHECK');

  v_cutoff_tz  := SYSTIMESTAMP AT TIME ZONE 'UTC';
  v_cutoff_utc := CAST(v_cutoff_tz AS TIMESTAMP);
  -- Informativo: el reset genera su propio RESET_CUTOFF_UTC al ejecutarse.
  put('PRECHECK_CUTOFF_UTC=' || TO_CHAR(v_cutoff_tz, 'YYYY-MM-DD"T"HH24:MI:SS.FF6"Z"'));

  v_pdb    := SYS_CONTEXT('USERENV', 'CON_NAME');
  v_schema := SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');
  v_user   := SYS_CONTEXT('USERENV', 'SESSION_USER');
  put('CURRENT_PDB=' || v_pdb);
  put('CURRENT_SCHEMA=' || v_schema);
  put('ORACLE_VERSION=' || DBMS_DB_VERSION.VERSION || '.' || DBMS_DB_VERSION.RELEASE);

  IF v_param_pdb IS NULL OR v_param_schema IS NULL THEN
    add_reason('PARAMETERS_MISSING');
  ELSIF NOT REGEXP_LIKE(v_param_pdb, '^[A-Za-z][A-Za-z0-9_$]{0,127}$')
     OR NOT REGEXP_LIKE(v_param_schema, '^[A-Za-z][A-Za-z0-9_$]{0,127}$') THEN
    add_reason('PARAMETER_FORMAT');
  ELSE
    IF v_pdb IS NULL OR v_pdb <> UPPER(v_param_pdb) THEN
      add_reason('PDB_MISMATCH');
    END IF;
    IF v_schema IS NULL OR v_schema <> UPPER(v_param_schema) THEN
      add_reason('SCHEMA_MISMATCH');
    END IF;
    IF v_user IS NULL OR v_user <> UPPER(v_param_schema) THEN
      add_reason('SESSION_USER_MISMATCH');
    END IF;
  END IF;

  v_expected_tables := c_delete_order MULTISET UNION ALL c_structural MULTISET UNION ALL c_other_preserved;
  init_caps;
  put('INVENTORY_EXPECTED_TABLES=' || v_expected_tables.COUNT
      || ' DELETE_TABLES=' || c_delete_order.COUNT
      || ' ZERO_TABLES=' || (c_delete_order.COUNT - c_partial.COUNT)
      || ' PARTIAL_TABLES=' || c_partial.COUNT);
  IF v_expected_tables.COUNT <> c_expected_tables OR c_delete_order.COUNT <> c_expected_deletes THEN
    add_reason('SCRIPT_INVENTORY_INCONSISTENT');
  END IF;

  SELECT COUNT(*), MAX("MigrationId") INTO v_mig_count, v_mig_last FROM "__EFMigrationsHistory";
  put('MIGRATIONS_COUNT=' || v_mig_count || ' EXPECTED=' || c_expected_migrations);
  put('MIGRATIONS_LAST=' || NVL(v_mig_last, '-') || ' EXPECTED=' || c_last_migration);
  IF v_mig_count <> c_expected_migrations OR NVL(v_mig_last, '-') <> c_last_migration THEN
    add_reason('MIGRATIONS_MISMATCH');
  END IF;

  inspect_schema(v_schema_state);
  IF v_schema_state.missing_tables > 0 THEN
    add_reason('MISSING_TABLES');
  END IF;
  IF v_schema_state.unexpected_tables > 0 THEN
    add_reason('UNEXPECTED_TABLES');
  END IF;
  IF v_schema_state.user_tables <> c_expected_tables THEN
    add_reason('USER_TABLES_NOT_46');
  END IF;

  -- Tabla manual historica: obligatoria, con FIXED_AT TIMESTAMP WITH TIME ZONE,
  -- sin nulos y con la guarda UTC ejecutable.
  SELECT COUNT(*) INTO v_aux_present FROM USER_TABLES WHERE TABLE_NAME = c_aux_table;
  put('AUX_TABLE_PRESENT=' || v_aux_present);
  IF v_aux_present <> 1 THEN
    add_reason('AUX_TABLE_MISSING');
  ELSE
    SELECT COUNT(*), MAX(DATA_TYPE) INTO v_n, v_aux_type
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = c_aux_table
       AND COLUMN_NAME = 'FIXED_AT';
    put('AUX_FIXED_AT_COLUMN=' || v_n);
    put('AUX_FIXED_AT_TYPE=' || NVL(v_aux_type, '-'));
    IF v_n <> 1 THEN
      add_reason('AUX_FIXED_AT_MISSING');
    ELSIF v_aux_type NOT LIKE 'TIMESTAMP(_) WITH TIME ZONE' THEN
      add_reason('AUX_FIXED_AT_TYPE_UNSUPPORTED');
    ELSE
      EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || qn(c_aux_table) || ' WHERE FIXED_AT IS NULL' INTO v_n;
      put('AUX_FIXED_AT_NULL_ROWS=' || v_n);
      IF v_n > 0 THEN
        add_reason('AUX_FIXED_AT_NULL_ROWS');
      END IF;
      BEGIN
        v_n := count_created_after(c_aux_table, v_cutoff_utc);
        v_aux_ok := TRUE;
      EXCEPTION
        WHEN OTHERS THEN
          add_reason('AUX_UTC_GUARD_NOT_EXECUTABLE');
      END;
    END IF;
  END IF;
  put('AUX_UTC_GUARD_EXECUTABLE=' || CASE WHEN v_aux_ok THEN 'YES' ELSE 'NO' END);
  IF v_schema_state.fk_unexpected > 0
     OR v_schema_state.fk_order_violation > 0
     OR v_schema_state.fk_pairs_found <> c_fk_pairs.COUNT THEN
    add_reason('FK_INVENTORY_MISMATCH');
  END IF;
  IF v_schema_state.constraints_bad > 0 OR v_schema_state.indexes_bad > 0 THEN
    add_reason('CONSTRAINTS_OR_INDEXES_NOT_VALID');
  END IF;
  IF v_schema_state.invalid_objects > 0 THEN
    add_reason('INVALID_OBJECTS');
  END IF;

  IF check_roles > 0 THEN
    add_reason('ROLES_MISMATCH');
  END IF;

  IF check_superadmin(v_sa_user_id) > 0 THEN
    add_reason('SUPERADMIN_NOT_EXACTLY_ONE_VALID');
  END IF;

  snapshot_structure(v_structure);
  print_structure(v_structure);
  put('BASELINE_COUNTS=' || baseline_token(v_structure));
  IF structure_failures(v_structure) > 0 THEN
    add_reason('STRUCTURAL_CATALOGS_MISMATCH');
  END IF;

  SELECT COUNT(*) INTO v_n FROM HOSPITALIZATION_SETTINGS;
  put('TABLE_COUNT[HOSPITALIZATION_SETTINGS]=' || v_n || ' EXPECTED=0');
  IF v_n <> 0 THEN
    add_reason('HOSPITALIZATION_SETTINGS_NOT_EMPTY');
  END IF;

  IF v_sa_user_id IS NOT NULL THEN
    SELECT COUNT(*) INTO v_sa_agents FROM AGENT_HUMANS WHERE USER_ID = v_sa_user_id;
  END IF;
  put('AGENT_HUMANS_LINKED_TO_SUPERADMIN=' || v_sa_agents);
  IF v_sa_agents > 1 THEN
    add_reason('AGENT_HUMANS_SUPERADMIN_AMBIGUOUS');
  END IF;
  v_keep_sa_agent := CASE WHEN v_sa_agents = 1 THEN 1 ELSE 0 END;
  put('AGENT_HUMANS_KEEP_SUPERADMIN_ROW=' || v_keep_sa_agent);

  v_planned := 0;
  FOR i IN 1 .. c_delete_order.COUNT LOOP
    IF NOT table_present(c_delete_order(i)) THEN
      v_counts(c_delete_order(i)) := 0;
      put('TABLE_COUNT[' || c_delete_order(i) || ']=MISSING CAP=' || v_caps(c_delete_order(i)));
    ELSE
      v_n := count_rows(c_delete_order(i));
      v_counts(c_delete_order(i)) := v_n;
      put('TABLE_COUNT[' || c_delete_order(i) || ']=' || v_n || ' CAP=' || v_caps(c_delete_order(i)));
      IF v_n > v_caps(c_delete_order(i)) THEN
        add_reason('VOLUME_CAP_EXCEEDED:' || c_delete_order(i));
      END IF;
      IF pos(c_delete_order(i), c_partial) = 0 THEN
        v_planned := v_planned + v_n;
      END IF;
    END IF;
  END LOOP;

  v_total := 0;
  FOR i IN 1 .. c_delete_order.COUNT LOOP
    IF NOT table_present(c_delete_order(i))
       OR (c_delete_order(i) = c_aux_table AND NOT v_aux_ok) THEN
      put('CREATED_AFTER_CUTOFF[' || c_delete_order(i) || ']=NOT_EVALUATED');
    ELSE
      v_n := count_created_after(c_delete_order(i), v_cutoff_utc);
      IF v_n > 0 THEN
        put('CREATED_AFTER_CUTOFF[' || c_delete_order(i) || ']=' || v_n);
      END IF;
      v_total := v_total + v_n;
    END IF;
  END LOOP;
  put('CREATED_AFTER_CUTOFF_TOTAL=' || v_total);
  IF v_total > 0 THEN
    add_reason('ROWS_CREATED_AFTER_CUTOFF');
  END IF;

  -- Filas que el reset planificaria en tablas parciales y estado final aprobado:
  -- USERS = 1, ROLES = 5, ROLE_PERMISSIONS = permisos actuales de los cinco roles.
  v_planned := v_planned + (v_counts('AGENT_HUMANS') - v_keep_sa_agent);
  v_n := CASE WHEN v_sa_user_id IS NOT NULL THEN 1 ELSE 0 END;
  v_planned := v_planned + (v_counts('USERS') - v_n);
  put('PLANNED_KEEP[USERS]=' || v_n || ' EXPECTED=1');
  IF v_n <> 1 THEN
    add_reason('PLANNED_USERS_NOT_1');
  END IF;

  SELECT COUNT(*) INTO v_n FROM ROLE_PERMISSIONS WHERE ROLE_ID = c_client_role_id;
  v_planned := v_planned + v_n;
  put('PLANNED_KEEP[ROLE_PERMISSIONS]=' || (v_counts('ROLE_PERMISSIONS') - v_n)
      || ' EXPECTED=' || v_structure('PERMISSIONS_VALID'));
  IF v_counts('ROLE_PERMISSIONS') - v_n <> v_structure('PERMISSIONS_VALID') THEN
    add_reason('PLANNED_PERMISSIONS_NOT_ONLY_APPROVED_ROLES');
  END IF;

  SELECT COUNT(*) INTO v_n FROM ROLES WHERE ROLE_ID = c_client_role_id;
  v_planned := v_planned + v_n;
  put('PLANNED_KEEP[ROLES]=' || (v_counts('ROLES') - v_n) || ' EXPECTED=5');
  IF v_counts('ROLES') - v_n <> 5 THEN
    add_reason('PLANNED_ROLES_NOT_5');
  END IF;

  put('PLANNED_KEEP[AGENT_HUMANS]=' || v_keep_sa_agent);
  put('PLANNED_DELETE_TOTAL=' || v_planned);
  put('DATA_STATE=' || CASE WHEN v_planned = 0 THEN 'ALREADY_RESET' ELSE 'PENDING_RESET' END);

  put('NOT_READY_REASONS_TOTAL=' || v_reasons);
  IF v_reasons = 0 THEN
    put('PRECHECK_RESULT=READY');
    :b_exit := 0;
  ELSE
    put('PRECHECK_RESULT=NOT_READY');
    :b_exit := 3;
  END IF;
  DBMS_APPLICATION_INFO.SET_MODULE(NULL, NULL);
END;
/

EXIT :b_exit ROLLBACK
