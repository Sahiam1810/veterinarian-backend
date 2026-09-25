-- Promueve un usuario interno existente al rol canónico SuperAdmin.
-- Uso SQL*Plus: @database/admin/promote_superadmin.sql correo@dominio.com
-- No crea usuarios ni contraseñas y revoca los refresh tokens existentes.
-- U5: USER_ACCOUNTS/USER_CREDENTIALS se fusionaron en USERS; USER_TOKENS ya
-- referencia USER_ID directamente en vez de ACCOUNT_ID.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;
SET DEFINE ON;
SET VERIFY OFF;
DEFINE target_email = '&1';

DECLARE
    v_user_id USERS.USER_ID%TYPE;
    v_matches NUMBER;
    v_role_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO v_role_count
    FROM ROLES
    WHERE ROLE_ID = '99999999-9999-9999-9999-999999999999'
      AND NAME = 'SuperAdmin';

    IF v_role_count <> 1 THEN
        RAISE_APPLICATION_ERROR(
            -20001,
            'No existe el rol canónico SuperAdmin. Ejecute primero database/seeds/extra/roles_seed.sql.');
    END IF;

    SELECT COUNT(*)
    INTO v_matches
    FROM USERS
    WHERE LOWER(EMAIL) = LOWER(TRIM('&&target_email'))
      AND IS_ACTIVE = 1
      AND PASSWORD_HASH IS NOT NULL
      AND PASSWORD_HASH != '';

    IF v_matches <> 1 THEN
        RAISE_APPLICATION_ERROR(
            -20002,
            'Debe existir exactamente un usuario activo con contraseña para el correo indicado.');
    END IF;

    SELECT USER_ID
    INTO v_user_id
    FROM USERS
    WHERE LOWER(EMAIL) = LOWER(TRIM('&&target_email'))
      AND IS_ACTIVE = 1
      AND PASSWORD_HASH IS NOT NULL
      AND PASSWORD_HASH != '';

    UPDATE USERS
    SET ROLE_ID = '99999999-9999-9999-9999-999999999999',
        UPDATED_AT = SYSTIMESTAMP
    WHERE USER_ID = v_user_id;

    DELETE FROM USER_TOKENS
    WHERE USER_ID = v_user_id;

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Usuario promovido. Inicie sesión nuevamente para obtener un JWT actualizado.');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

UNDEFINE target_email;
EXIT SUCCESS;
