-- Otorga al rol Auxiliar el permiso de solo-lectura sobre el modulo
-- "Veterinarios", que nunca existio ni en role_permissions_seed.sql ni en
-- role_permissions_repair_2026-09-10.sql (a diferencia de Recepcionista,
-- que si lo tiene). Sin este permiso, GET /api/Veterinarians devuelve 403
-- para Auxiliar y el formulario de "Nueva Cita" del modulo Auxiliar
-- sustituye el campo "Profesional" por nombres de relleno en vez de los
-- veterinarios reales creados desde el panel de SuperAdmin.
--
-- role_permissions_seed.sql ya declara esta misma fila (ver seccion
-- Auxiliar) para que cualquier base nueva la reciba de forma automatica;
-- este patch aplica el mismo valor a bases ya sembradas.
SET DEFINE OFF
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
  v_user    VARCHAR2(128);
  v_schema  VARCHAR2(128);
  v_pdb     VARCHAR2(128);
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'SESSION_USER'),
         SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'),
         SYS_CONTEXT('USERENV', 'CON_NAME')
    INTO v_user, v_schema, v_pdb
    FROM DUAL;

  IF v_user <> 'VET_APP' OR v_schema <> 'VET_APP' OR v_pdb <> 'FREEPDB1' THEN
    RAISE_APPLICATION_ERROR(
      -20001,
      'Target must be VET_APP@FREEPDB1. Current target: '
      || v_user || '/' || v_schema || '@' || v_pdb);
  END IF;
END;
/

DECLARE
  v_role_id   VARCHAR2(36) := '66666666-6666-6666-6666-666666666666';
  v_module_id VARCHAR2(36);
  v_count     NUMBER;
BEGIN
  SELECT MODULE_ID INTO v_module_id FROM MODULES WHERE NAME = 'Veterinarios';

  SELECT COUNT(*) INTO v_count
    FROM ROLE_PERMISSIONS
   WHERE ROLE_ID = v_role_id AND MODULE_ID = v_module_id;

  IF v_count = 0 THEN
    INSERT INTO ROLE_PERMISSIONS (
      ROLE_PERMISSION_ID, ROLE_ID, MODULE_ID,
      CAN_VIEW, CAN_CREATE, CAN_EDIT, CAN_DELETE, CREATED_AT)
    VALUES (
      'c1a2b3c4-d5e6-47f8-9012-3456789abcde', v_role_id, v_module_id,
      1, 0, 0, 0, SYSTIMESTAMP);
    DBMS_OUTPUT.PUT_LINE('OK: permiso Auxiliar/Veterinarios creado');
  ELSE
    UPDATE ROLE_PERMISSIONS
       SET CAN_VIEW = 1
     WHERE ROLE_ID = v_role_id AND MODULE_ID = v_module_id AND CAN_VIEW = 0;
    DBMS_OUTPUT.PUT_LINE('OK: permiso Auxiliar/Veterinarios ya existia (CAN_VIEW asegurado en 1)');
  END IF;
END;
/

COMMIT;
SELECT r.NAME AS ROLE_NAME, m.NAME AS MODULE_NAME, rp.CAN_VIEW
  FROM ROLE_PERMISSIONS rp
  JOIN ROLES r ON r.ROLE_ID = rp.ROLE_ID
  JOIN MODULES m ON m.MODULE_ID = rp.MODULE_ID
 WHERE r.NAME = 'Auxiliar' AND m.NAME = 'Veterinarios';
EXIT
