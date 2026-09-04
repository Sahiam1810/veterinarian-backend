-- Promueve una cuenta interna existente al rol canónico SuperAdmin.
-- Uso SQL*Plus: @database/admin/promote_superadmin.sql correo@dominio.com
-- No crea usuarios ni credenciales y revoca los refresh tokens existentes.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;
SET VERIFY OFF;
DEFINE target_email = '&1';

DECLARE
    v_account_id USER_ACCOUNTS.ACCOUNT_ID%TYPE;
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
            'No existe el rol canónico SuperAdmin. Ejecute primero database/seeds/roles_seed.sql.');
    END IF;

    SELECT COUNT(*)
    INTO v_matches
    FROM USERS users_table
    JOIN USER_ACCOUNTS accounts
      ON accounts.USER_ID = users_table.USER_ID
    JOIN USER_CREDENTIALS credentials
      ON credentials.ACCOUNT_ID = accounts.ACCOUNT_ID
    WHERE LOWER(accounts.MAIL) = LOWER(TRIM('&&target_email'))
      AND accounts.STATUS = 'Activo'
      AND users_table.IS_ACTIVE = 1;

    IF v_matches <> 1 THEN
        RAISE_APPLICATION_ERROR(
            -20002,
            'Debe existir exactamente una cuenta activa con credencial para el correo indicado.');
    END IF;

    SELECT accounts.ACCOUNT_ID, users_table.USER_ID
    INTO v_account_id, v_user_id
    FROM USERS users_table
    JOIN USER_ACCOUNTS accounts
      ON accounts.USER_ID = users_table.USER_ID
    JOIN USER_CREDENTIALS credentials
      ON credentials.ACCOUNT_ID = accounts.ACCOUNT_ID
    WHERE LOWER(accounts.MAIL) = LOWER(TRIM('&&target_email'))
      AND accounts.STATUS = 'Activo'
      AND users_table.IS_ACTIVE = 1;

    UPDATE USERS
    SET ROLE_ID = '99999999-9999-9999-9999-999999999999',
        UPDATED_AT = SYSTIMESTAMP
    WHERE USER_ID = v_user_id;

    DELETE FROM USER_TOKENS
    WHERE ACCOUNT_ID = v_account_id;

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Cuenta promovida. Inicie sesión nuevamente para obtener un JWT actualizado.');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

UNDEFINE target_email;
EXIT SUCCESS;
