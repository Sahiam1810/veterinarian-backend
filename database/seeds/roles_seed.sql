-- Catálogo inicial de roles del sistema veterinario.
-- Este script se ejecuta directamente contra la base de datos Oracle
-- (por ejemplo con SQL*Plus o SQL Developer), fuera de la aplicación .NET.
--
-- Configuración de Encoding / Sesión para SQL*Plus:
SET DEFINE OFF;
ALTER SESSION SET NLS_LANGUAGE = 'SPANISH';
ALTER SESSION SET NLS_TERRITORY = 'SPAIN';

-- Requisito: la tabla ROLES debe existir (migraciones InitialCreate +
-- RolesMigration + RemoveRolesCodeSeed ya aplicadas).
--
-- Es idempotente: usa MERGE para no duplicar filas si se ejecuta más de una vez.
-- Los ROLE_ID son fijos para garantizar consistencia con role_permissions_seed.sql.

-- 1. Administrador
MERGE INTO ROLES target
USING (SELECT '11111111-1111-1111-1111-111111111111' AS ID FROM DUAL) source
ON (target.ROLE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Administrador',
            'Configura el sistema, gestiona usuarios, roles y permisos; ve toda la operación',
            SYSTIMESTAMP);

-- 2. Veterinario
MERGE INTO ROLES target
USING (SELECT '44444444-4444-4444-4444-444444444444' AS ID FROM DUAL) source
ON (target.ROLE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Veterinario',
            'Consulta su agenda, atiende citas y registra la historia clínica de la mascota',
            SYSTIMESTAMP);

-- 3. Recepcionista
MERGE INTO ROLES target
USING (SELECT '55555555-5555-5555-5555-555555555555' AS ID FROM DUAL) source
ON (target.ROLE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Recepcionista',
            'Registra dueños y mascotas, agenda, reprograma y cancela citas',
            SYSTIMESTAMP);

-- 4. Auxiliar
MERGE INTO ROLES target
USING (SELECT '66666666-6666-6666-6666-666666666666' AS ID FROM DUAL) source
ON (target.ROLE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Auxiliar',
            'Apoya el registro y la preparación de la atención, según permisos asignados',
            SYSTIMESTAMP);

-- 5. Cliente
MERGE INTO ROLES target
USING (SELECT '77777777-7777-7777-7777-777777777777' AS ID FROM DUAL) source
ON (target.ROLE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Cliente',
            'Cliente (dueño de mascota) que interactúa con el sistema a través del chatbot',
            SYSTIMESTAMP);

COMMIT;
