-- Otorga al rol Auxiliar el permiso de creacion sobre el modulo "Citas".
-- El seed original lo declaraba deliberadamente solo de lectura
-- (CAN_VIEW=1, CAN_CREATE=0): el auxiliar podia ver la agenda pero no
-- crear citas. Se decidio ampliarlo a CAN_CREATE=1 porque el modulo
-- Auxiliar ya tiene en su interfaz un panel "Nueva Cita" pensado para
-- que el auxiliar registre citas y prepare peso/temperatura/instrumental
-- antes de la atencion. Sin este permiso, POST /api/Appointments
-- devuelve 403 para Auxiliar.
--
-- role_permissions_seed.sql ya declara este mismo valor (CAN_CREATE=1)
-- para que cualquier base nueva lo reciba de forma automatica; este
-- patch aplica el mismo valor a bases ya sembradas donde la fila ya
-- existia con CAN_CREATE=0.
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
     SET CAN_CREATE = 1
   WHERE ROLE_ID = (SELECT ROLE_ID FROM ROLES WHERE NAME = 'Auxiliar')
     AND MODULE_ID = (SELECT MODULE_ID FROM MODULES WHERE NAME = 'Citas')
     AND CAN_CREATE = 0;
  v_rows := SQL%ROWCOUNT;
  IF v_rows > 0 THEN
    DBMS_OUTPUT.PUT_LINE('OK: CAN_CREATE otorgado en ' || v_rows || ' fila(s)');
  ELSE
    DBMS_OUTPUT.PUT_LINE('OK: sin cambios (ya estaba en 1 o rol/modulo ausente)');
  END IF;
END;
/

COMMIT;
SELECT r.NAME AS ROLE_NAME, m.NAME AS MODULE_NAME, rp.CAN_VIEW, rp.CAN_CREATE
  FROM ROLE_PERMISSIONS rp
  JOIN ROLES r ON r.ROLE_ID = rp.ROLE_ID
  JOIN MODULES m ON m.MODULE_ID = rp.MODULE_ID
 WHERE r.NAME = 'Auxiliar' AND m.NAME = 'Citas';
EXIT
