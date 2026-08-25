-- Catálogo inicial de roles del sistema veterinario.
-- Este script se ejecuta directamente contra la base de datos Oracle
-- (por ejemplo con SQL*Plus o SQL Developer), fuera de la aplicación .NET.
-- Los roles NO están quemados en el código: el administrador puede crear,
-- editar o eliminar roles adicionales desde la propia aplicación una vez
-- desplegada (módulo 3 - Roles y permisos configurables).
--
-- Requisito: la tabla ROLES debe existir (migraciones InitialCreate +
-- RolesMigration + RemoveRolesCodeSeed ya aplicadas).

INSERT INTO ROLES (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
VALUES ('11111111-1111-1111-1111-111111111111', 'Administrador',
        'Configura el sistema, gestiona usuarios, roles y permisos; ve toda la operación',
        SYSTIMESTAMP);

INSERT INTO ROLES (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
VALUES ('44444444-4444-4444-4444-444444444444', 'Veterinario',
        'Consulta su agenda, atiende citas y registra la historia clínica de la mascota',
        SYSTIMESTAMP);

INSERT INTO ROLES (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
VALUES ('55555555-5555-5555-5555-555555555555', 'Recepcionista',
        'Registra dueños y mascotas, agenda, reprograma y cancela citas',
        SYSTIMESTAMP);

INSERT INTO ROLES (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
VALUES ('66666666-6666-6666-6666-666666666666', 'Auxiliar',
        'Apoya el registro y la preparación de la atención, según permisos asignados',
        SYSTIMESTAMP);

INSERT INTO ROLES (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
VALUES ('77777777-7777-7777-7777-777777777777', 'Cliente',
        'Portal para ver sus mascotas y sus citas',
        SYSTIMESTAMP);

COMMIT;
