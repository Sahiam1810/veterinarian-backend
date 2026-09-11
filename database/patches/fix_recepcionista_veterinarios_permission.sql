-- Corrige un permiso de rol desincronizado: la fila ROLE_PERMISSIONS de
-- (Recepcionista, Veterinarios) quedo con CAN_VIEW=0 en bases ya sembradas,
-- aunque database/seeds/extra/role_permissions_seed.sql declara CAN_VIEW=1.
-- role_permissions_seed.sql solo inserta permisos ausentes (MERGE ... WHEN
-- NOT MATCHED), por lo que reejecutar el seed no corrige filas existentes
-- con un valor distinto: se requiere este patch idempotente.
--
-- Efecto observado: GET /api/Veterinarians devuelve 403 para Recepcionista,
-- el modulo de Agenda y Citas del frontend oculta el error y sustituye la
-- lista de profesionales por un veterinario ficticio con id no-GUID
-- ("pro-default"), lo que hace fallar POST /api/Appointments con
-- "The request field is required." al no poder convertir VeterinarianId.
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
  v_rows NUMBER;
BEGIN
  UPDATE ROLE_PERMISSIONS
     SET CAN_VIEW = 1
   WHERE ROLE_ID = (SELECT ROLE_ID FROM ROLES WHERE NAME = 'Recepcionista')
     AND MODULE_ID = (SELECT MODULE_ID FROM MODULES WHERE NAME = 'Veterinarios')
     AND CAN_VIEW = 0;
  v_rows := SQL%ROWCOUNT;
  IF v_rows > 0 THEN
    DBMS_OUTPUT.PUT_LINE('OK: CAN_VIEW corregido en ' || v_rows || ' fila(s)');
  ELSE
    DBMS_OUTPUT.PUT_LINE('OK: sin cambios (ya estaba en 1 o rol/modulo ausente)');
  END IF;
END;
/

COMMIT;
SELECT r.NAME AS ROLE_NAME, m.NAME AS MODULE_NAME, rp.CAN_VIEW
  FROM ROLE_PERMISSIONS rp
  JOIN ROLES r ON r.ROLE_ID = rp.ROLE_ID
  JOIN MODULES m ON m.MODULE_ID = rp.MODULE_ID
 WHERE r.NAME = 'Recepcionista' AND m.NAME = 'Veterinarios';
EXIT
