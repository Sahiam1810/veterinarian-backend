-- =============================================================================
-- INSERT ALL SEEDS (CORREGIDO Y BLINDADO CONTRA ORA-02291)
-- Base de datos: Oracle Database
-- =============================================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;
SET DEFINE OFF;
ALTER SESSION SET NLS_LANGUAGE = 'SPANISH';
ALTER SESSION SET NLS_TERRITORY = 'SPAIN';

-- =============================================================================
-- Nivel 1: Catálogos base (sin dependencias)
-- =============================================================================

-- 1. ROLES (Compara por NAME para sincronizar con los IDs canónicos)
MERGE INTO ROLES target
USING (
    SELECT '99999999-9999-9999-9999-999999999999' AS ID, 'SuperAdmin' AS NAME,
           'Rol de sistema con autoridad no delegable para seguridad y permisos' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT '11111111-1111-1111-1111-111111111111' AS ID, 'Administrador' AS NAME,
           'Configura el sistema, gestiona usuarios, roles y permisos; ve toda la operación' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT '44444444-4444-4444-4444-444444444444' AS ID, 'Veterinario' AS NAME,
           'Consulta su agenda, atiende citas y registra la historia clínica de la mascota' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT '55555555-5555-5555-5555-555555555555' AS ID, 'Recepcionista' AS NAME,
           'Registra dueños y mascotas, agenda, reprograma y cancela citas' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT '66666666-6666-6666-6666-666666666666' AS ID, 'Auxiliar' AS NAME,
           'Apoya el registro y la preparación de la atención, según permisos asignados' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT '77777777-7777-7777-7777-777777777777' AS ID, 'Cliente' AS NAME,
           'Cliente (dueño de mascota) que interactúa con el sistema a través del chatbot' AS DESCRIPTION FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN
    UPDATE SET target.ROLE_ID = source.ID, target.DESCRIPTION = source.DESCRIPTION
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

COMMIT;

-- 2. MODULES (modules_seed.sql)
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000001' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Clientes', 'Gestión de clientes (dueños de mascotas)', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000002' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Mascotas', 'Gestión de mascotas registradas', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000003' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Especies y Razas', 'Catálogo de especies y razas de mascotas', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000004' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Especialidades', 'Catálogo de especialidades veterinarias', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000005' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Veterinarios', 'Gestión de veterinarios del sistema', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000006' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Citas', 'Gestión de citas médicas veterinarias', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000007' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Historiales Clínicos', 'Historias médicas, vacunas y diagnósticos', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000008' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Servicios', 'Catálogo de servicios y tipos de servicio', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000009' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Estados de Cita', 'Catálogo de estados posibles de una cita', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000010' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Cuentas y Pagos', 'Estados de cuenta y pagos de clientes', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000011' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Notificaciones', 'Gestión de notificaciones del sistema', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000012' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Usuarios', 'Gestión de usuarios, cuentas y credenciales', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000013' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Roles', 'Gestión de roles del sistema', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000014' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Disponibilidades', 'Gestión de disponibilidades de veterinarios', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000015' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Relación Clientes-Mascotas', 'Asociaciones entre clientes y mascotas', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000016' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Permisos', 'Gestión de permisos por rol y por usuario', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000017' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Chat'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Chat', 'Gestión de conversaciones, participantes y mensajes', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000018' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Escalamientos'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Escalamientos', 'Gestión y seguimiento de conversaciones escaladas', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000019' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('IA y Agente'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'IA y Agente', 'Ejecuciones, modelos y configuración del agente conversacional', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000020' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Catálogos del Chat'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Catálogos del Chat', 'Estados, tipos y prioridades del subsistema conversacional', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000021' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Reportes'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Reportes', 'Reportes operativos y de gestión', SYSTIMESTAMP);

COMMIT;

-- 3. SPECIES (veterinary_catalogs_seed.sql)
MERGE INTO SPECIES target
USING (
    SELECT '88000000-0000-0000-0000-000000000001' ID, 'Perro' NAME FROM DUAL UNION ALL
    SELECT '88000000-0000-0000-0000-000000000002', 'Gato' FROM DUAL UNION ALL
    SELECT '88000000-0000-0000-0000-000000000004', 'Ave' FROM DUAL UNION ALL
    SELECT '88000000-0000-0000-0000-000000000005', 'Conejo' FROM DUAL UNION ALL
    SELECT '88000000-0000-0000-0000-000000000003', 'Otro' FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN UPDATE SET target.SPECIES_ID = source.ID
WHEN NOT MATCHED THEN
    INSERT (SPECIES_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 4. SPECIALTIES (veterinary_catalogs_seed.sql)
MERGE INTO SPECIALTIES target
USING (
    SELECT '89000000-0000-0000-0000-000000000001' ID, 'Medicina general' NAME,
           'Atención veterinaria general' DESCRIPTION FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000002', 'Cirugía',
           'Procedimientos quirúrgicos veterinarios' FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000003', 'Dermatología',
           'Diagnóstico y tratamiento dermatológico' FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000004', 'Medicina interna',
           'Diagnóstico y tratamiento de enfermedades internas' FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000005', 'Urgencias',
           'Atención clínica veterinaria prioritaria' FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN UPDATE SET target.SPECIALTY_ID = source.ID, target.DESCRIPTION = source.DESCRIPTION
WHEN NOT MATCHED THEN
    INSERT (SPECIALTY_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

COMMIT;

-- 5. TYPE_SERVICES (veterinary_catalogs_seed.sql)
MERGE INTO TYPE_SERVICES target
USING (
    SELECT '87000000-0000-0000-0000-000000000001' ID, 'Consulta' NAME,
           'Valoración veterinaria general o especializada' DESCRIPTION FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000002', 'Vacunación',
           'Aplicación y seguimiento de vacunas' FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000003', 'Procedimiento',
           'Procedimiento ambulatorio o quirúrgico' FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000004', 'Diagnóstico',
           'Pruebas y ayudas diagnósticas' FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000005', 'Urgencia',
           'Atención veterinaria prioritaria' FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN UPDATE SET target.TYPE_SERVICE_ID = source.ID, target.DESCRIPTION = source.DESCRIPTION
WHEN NOT MATCHED THEN
    INSERT (TYPE_SERVICE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

COMMIT;

-- 6. STATUS_APPOINTMENTS (status_appointments_seed.sql)
MERGE INTO STATUS_APPOINTMENTS target
USING (
    SELECT 'aaaaaaaa-0000-0000-0000-000000000001' AS ID, 'AGENDADA' AS NAME FROM DUAL UNION ALL
    SELECT 'aaaaaaaa-0000-0000-0000-000000000002' AS ID, 'ATENDIDA' AS NAME FROM DUAL UNION ALL
    SELECT 'aaaaaaaa-0000-0000-0000-000000000005' AS ID, 'CONFIRMADA' AS NAME FROM DUAL UNION ALL
    SELECT 'aaaaaaaa-0000-0000-0000-000000000006' AS ID, 'EN_PROGRESO' AS NAME FROM DUAL UNION ALL
    SELECT 'aaaaaaaa-0000-0000-0000-000000000003' AS ID, 'CANCELADA' AS NAME FROM DUAL UNION ALL
    SELECT 'aaaaaaaa-0000-0000-0000-000000000004' AS ID, 'NO_ASISTIO' AS NAME FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN UPDATE SET target.STATUS_APPOINTMENT_ID = source.ID
WHEN NOT MATCHED THEN
    INSERT (STATUS_APPOINTMENT_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 7. CONVERSATIONS_STATUSES (chat_conversation_catalogs_seed.sql)
MERGE INTO CONVERSATIONS_STATUSES target
USING (SELECT '81000000-0000-0000-0000-000000000001' ID, 'Abierta' NAME FROM DUAL) source
ON (target.CONVERSATIONS_STATUSES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_STATUS = source.NAME
WHEN NOT MATCHED THEN
    INSERT (CONVERSATIONS_STATUSES_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO CONVERSATIONS_STATUSES target
USING (SELECT '81000000-0000-0000-0000-000000000002' ID, 'En atención' NAME FROM DUAL) source
ON (target.CONVERSATIONS_STATUSES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_STATUS = source.NAME
WHEN NOT MATCHED THEN
    INSERT (CONVERSATIONS_STATUSES_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO CONVERSATIONS_STATUSES target
USING (SELECT '81000000-0000-0000-0000-000000000003' ID, 'Escalada' NAME FROM DUAL) source
ON (target.CONVERSATIONS_STATUSES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_STATUS = source.NAME
WHEN NOT MATCHED THEN
    INSERT (CONVERSATIONS_STATUSES_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO CONVERSATIONS_STATUSES target
USING (SELECT '81000000-0000-0000-0000-000000000004' ID, 'Cerrada' NAME FROM DUAL) source
ON (target.CONVERSATIONS_STATUSES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_STATUS = source.NAME
WHEN NOT MATCHED THEN
    INSERT (CONVERSATIONS_STATUSES_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 8. SENDER_TYPES (chat_conversation_catalogs_seed.sql)
MERGE INTO SENDER_TYPES target
USING (SELECT '82000000-0000-0000-0000-000000000001' ID, 'Cliente' NAME FROM DUAL) source
ON (target.SENDER_TYPES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_TYPE = source.NAME
WHEN NOT MATCHED THEN
    INSERT (SENDER_TYPES_ID, NAME_TYPE, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO SENDER_TYPES target
USING (SELECT '82000000-0000-0000-0000-000000000002' ID, 'Agente IA' NAME FROM DUAL) source
ON (target.SENDER_TYPES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_TYPE = source.NAME
WHEN NOT MATCHED THEN
    INSERT (SENDER_TYPES_ID, NAME_TYPE, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO SENDER_TYPES target
USING (SELECT '82000000-0000-0000-0000-000000000003' ID, 'Agente humano' NAME FROM DUAL) source
ON (target.SENDER_TYPES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_TYPE = source.NAME
WHEN NOT MATCHED THEN
    INSERT (SENDER_TYPES_ID, NAME_TYPE, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO SENDER_TYPES target
USING (SELECT '82000000-0000-0000-0000-000000000004' ID, 'Sistema' NAME FROM DUAL) source
ON (target.SENDER_TYPES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_TYPE = source.NAME
WHEN NOT MATCHED THEN
    INSERT (SENDER_TYPES_ID, NAME_TYPE, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 9. MESSAGE_TYPES (chat_runtime_catalogs_seed.sql)
MERGE INTO MESSAGE_TYPES target
USING (
    SELECT '83000000-0000-0000-0000-000000000001' ID, 'Texto' NAME FROM DUAL UNION ALL
    SELECT '83000000-0000-0000-0000-000000000002', 'Imagen' FROM DUAL UNION ALL
    SELECT '83000000-0000-0000-0000-000000000003', 'Audio' FROM DUAL UNION ALL
    SELECT '83000000-0000-0000-0000-000000000004', 'Documento' FROM DUAL UNION ALL
    SELECT '83000000-0000-0000-0000-000000000005', 'Sistema' FROM DUAL
) source
ON (target.MESSAGE_TYPES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_TYPE = source.NAME
WHEN NOT MATCHED THEN
    INSERT (MESSAGE_TYPES_ID, NAME_TYPE, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 10. PRIORITY (chat_runtime_catalogs_seed.sql)
MERGE INTO PRIORITY target
USING (
    SELECT '84000000-0000-0000-0000-000000000001' ID, 'Baja' NAME FROM DUAL UNION ALL
    SELECT '84000000-0000-0000-0000-000000000002', 'Media' FROM DUAL UNION ALL
    SELECT '84000000-0000-0000-0000-000000000003', 'Alta' FROM DUAL UNION ALL
    SELECT '84000000-0000-0000-0000-000000000004', 'Urgente' FROM DUAL
) source
ON (target.PRIORITY_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_PRIORITY = source.NAME
WHEN NOT MATCHED THEN
    INSERT (PRIORITY_ID, NAME_PRIORITY, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 11. ESCALATIONS_STATUSES (chat_runtime_catalogs_seed.sql)
MERGE INTO ESCALATIONS_STATUSES target
USING (
    SELECT '85000000-0000-0000-0000-000000000001' ID, 'Pendiente' NAME FROM DUAL UNION ALL
    SELECT '85000000-0000-0000-0000-000000000002', 'Asignada' FROM DUAL UNION ALL
    SELECT '85000000-0000-0000-0000-000000000003', 'En atención' FROM DUAL UNION ALL
    SELECT '85000000-0000-0000-0000-000000000004', 'Resuelta' FROM DUAL UNION ALL
    SELECT '85000000-0000-0000-0000-000000000005', 'Cancelada' FROM DUAL
) source
ON (target.ESCALATIONS_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_STATUS = source.NAME
WHEN NOT MATCHED THEN
    INSERT (ESCALATIONS_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- 12. AI_RUNS_STATUSES (chat_runtime_catalogs_seed.sql)
MERGE INTO AI_RUNS_STATUSES target
USING (
    SELECT '86000000-0000-0000-0000-000000000001' ID, 'Pendiente' NAME FROM DUAL UNION ALL
    SELECT '86000000-0000-0000-0000-000000000002', 'En ejecución' FROM DUAL UNION ALL
    SELECT '86000000-0000-0000-0000-000000000003', 'Completada' FROM DUAL UNION ALL
    SELECT '86000000-0000-0000-0000-000000000004', 'Fallida' FROM DUAL UNION ALL
    SELECT '86000000-0000-0000-0000-000000000005', 'Cancelada' FROM DUAL
) source
ON (target.AI_RUNS_STATUSES_ID = source.ID)
WHEN MATCHED THEN UPDATE SET target.NAME_STATUS = source.NAME
WHEN NOT MATCHED THEN
    INSERT (AI_RUNS_STATUSES_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- =============================================================================
-- Nivel 2: RACES (depende de SPECIES)
-- =============================================================================

-- 13. RACES (veterinary_catalogs_seed.sql)
MERGE INTO RACES target
USING (
    -- Perro
    SELECT '8a000000-0000-0000-0000-000000000001' ID, species.SPECIES_ID, 'Mestizo' NAME
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000002', species.SPECIES_ID, 'Labrador Retriever'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000003', species.SPECIES_ID, 'Golden Retriever'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000004', species.SPECIES_ID, 'Bulldog'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000005', species.SPECIES_ID, 'Poodle'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000011', species.SPECIES_ID, 'Pastor Alemán'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000012', species.SPECIES_ID, 'Beagle'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000013', species.SPECIES_ID, 'Chihuahua'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000014', species.SPECIES_ID, 'Husky Siberiano'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000015', species.SPECIES_ID, 'Boxer'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000016', species.SPECIES_ID, 'Cocker Spaniel'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000017', species.SPECIES_ID, 'Schnauzer'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('PERRO', 'CANINO') AND ROWNUM = 1 UNION ALL
    -- Gato
    SELECT '8a000000-0000-0000-0000-000000000006', species.SPECIES_ID, 'Mestizo'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000007', species.SPECIES_ID, 'Siamés'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000008', species.SPECIES_ID, 'Persa'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000009', species.SPECIES_ID, 'Maine Coon'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000018', species.SPECIES_ID, 'Bengalí'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000019', species.SPECIES_ID, 'Ragdoll'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000020', species.SPECIES_ID, 'Azul Ruso'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000021', species.SPECIES_ID, 'Angora'
    FROM SPECIES species WHERE UPPER(species.NAME) IN ('GATO', 'FELINO') AND ROWNUM = 1 UNION ALL
    -- Ave
    SELECT '8a000000-0000-0000-0000-000000000022', species.SPECIES_ID, 'Periquito'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'AVE' AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000023', species.SPECIES_ID, 'Canario'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'AVE' AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000024', species.SPECIES_ID, 'Cacatúa'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'AVE' AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000025', species.SPECIES_ID, 'Loro'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'AVE' AND ROWNUM = 1 UNION ALL
    -- Conejo
    SELECT '8a000000-0000-0000-0000-000000000026', species.SPECIES_ID, 'Holandés'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'CONEJO' AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000027', species.SPECIES_ID, 'Mini Rex'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'CONEJO' AND ROWNUM = 1 UNION ALL
    SELECT '8a000000-0000-0000-0000-000000000028', species.SPECIES_ID, 'Cabeza de León'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'CONEJO' AND ROWNUM = 1 UNION ALL
    -- Otro
    SELECT '8a000000-0000-0000-0000-000000000010', species.SPECIES_ID, 'No especificada'
    FROM SPECIES species WHERE UPPER(species.NAME) = 'OTRO' AND ROWNUM = 1
) source
ON (
    target.SPECIES_ID = source.SPECIES_ID
    AND UPPER(target.NAME) = UPPER(source.NAME)
)
WHEN NOT MATCHED THEN
    INSERT (RACE_ID, SPECIES_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.SPECIES_ID, source.NAME, SYSTIMESTAMP);

COMMIT;

-- =============================================================================
-- Nivel 3: Permisos (ROLE_PERMISSIONS con resolución dinámica segura)
-- =============================================================================

DECLARE
    PROCEDURE ensure_permission(
        p_id VARCHAR2,
        p_role_id VARCHAR2,
        p_module_name VARCHAR2,
        p_can_view NUMBER,
        p_can_create NUMBER,
        p_can_edit NUMBER,
        p_can_delete NUMBER) IS
        v_actual_role_id VARCHAR2(36);
        v_module_id VARCHAR2(36);
    BEGIN
        -- 1. Resuelve el ROLE_ID real en la tabla ROLES (evita ORA-02291)
        BEGIN
            SELECT ROLE_ID INTO v_actual_role_id
            FROM ROLES
            WHERE ROLE_ID = p_role_id
               OR UPPER(NAME) = CASE
                    WHEN p_role_id = '11111111-1111-1111-1111-111111111111' THEN 'ADMINISTRADOR'
                    WHEN p_role_id = '44444444-4444-4444-4444-444444444444' THEN 'VETERINARIO'
                    WHEN p_role_id = '55555555-5555-5555-5555-555555555555' THEN 'RECEPCIONISTA'
                    WHEN p_role_id = '66666666-6666-6666-6666-666666666666' THEN 'AUXILIAR'
                    WHEN p_role_id = '77777777-7777-7777-7777-777777777777' THEN 'CLIENTE'
                    WHEN p_role_id = '99999999-9999-9999-9999-999999999999' THEN 'SUPERADMIN'
                    ELSE 'DESCONOCIDO'
                  END
            FETCH FIRST 1 ROWS ONLY;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_actual_role_id := NULL;
        END;

        -- 2. Resuelve el MODULE_ID real en la tabla MODULES
        BEGIN
            SELECT MODULE_ID INTO v_module_id
            FROM MODULES
            WHERE UPPER(NAME) = UPPER(p_module_name)
            FETCH FIRST 1 ROWS ONLY;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_module_id := NULL;
        END;

        -- 3. Solo inserta si ambos existen
        IF v_actual_role_id IS NOT NULL AND v_module_id IS NOT NULL THEN
            MERGE INTO ROLE_PERMISSIONS target
            USING (
                SELECT p_id AS ID,
                       v_actual_role_id AS ROLE_ID,
                       v_module_id AS MODULE_ID,
                       p_can_view AS CAN_VIEW,
                       p_can_create AS CAN_CREATE,
                       p_can_edit AS CAN_EDIT,
                       p_can_delete AS CAN_DELETE
                FROM DUAL
            ) source
            ON (target.ROLE_ID = source.ROLE_ID AND target.MODULE_ID = source.MODULE_ID)
            WHEN MATCHED THEN
                UPDATE SET target.CAN_VIEW = source.CAN_VIEW,
                           target.CAN_CREATE = source.CAN_CREATE,
                           target.CAN_EDIT = source.CAN_EDIT,
                           target.CAN_DELETE = source.CAN_DELETE
            WHEN NOT MATCHED THEN
                INSERT (
                    ROLE_PERMISSION_ID, ROLE_ID, MODULE_ID,
                    CAN_VIEW, CAN_CREATE, CAN_EDIT, CAN_DELETE, CREATED_AT)
                VALUES (
                    source.ID, source.ROLE_ID, source.MODULE_ID,
                    source.CAN_VIEW, source.CAN_CREATE,
                    source.CAN_EDIT, source.CAN_DELETE, SYSTIMESTAMP);
        END IF;
    END;
BEGIN
    -- Administrador
    ensure_permission('a4b4bb3e-3516-4b6c-a577-3ec4d65c3a7b', '11111111-1111-1111-1111-111111111111', 'Clientes', 1, 1, 1, 1);
    ensure_permission('7f3fdc4f-b845-4403-aa3c-209fb0d14968', '11111111-1111-1111-1111-111111111111', 'Mascotas', 1, 1, 1, 1);
    ensure_permission('9dc06609-4a2f-4980-8d3b-498e3e9ef622', '11111111-1111-1111-1111-111111111111', 'Especies y Razas', 1, 1, 1, 1);
    ensure_permission('204086ef-1ac5-4c4a-86d9-0ab0125b617e', '11111111-1111-1111-1111-111111111111', 'Especialidades', 1, 1, 1, 1);
    ensure_permission('b95efcce-cc93-4474-be52-2676bc132f73', '11111111-1111-1111-1111-111111111111', 'Veterinarios', 1, 1, 1, 1);
    ensure_permission('c89cdbba-e1f7-4ff8-becd-c39ac914475e', '11111111-1111-1111-1111-111111111111', 'Citas', 1, 1, 1, 1);
    ensure_permission('3b9dde6d-68e1-4470-b82c-327feb02f4f6', '11111111-1111-1111-1111-111111111111', 'Historiales Clínicos', 1, 1, 1, 1);
    ensure_permission('d704a878-f7a3-4fa7-b20a-3450292720a7', '11111111-1111-1111-1111-111111111111', 'Servicios', 1, 1, 1, 1);
    ensure_permission('ab64891f-bba9-4e1f-8901-8aa225643550', '11111111-1111-1111-1111-111111111111', 'Estados de Cita', 1, 1, 1, 1);
    ensure_permission('4ac22e89-a9a5-4576-8c80-97c000dddf12', '11111111-1111-1111-1111-111111111111', 'Cuentas y Pagos', 1, 1, 1, 1);
    ensure_permission('73ae229d-f977-46f1-bfd5-42c7e92731f9', '11111111-1111-1111-1111-111111111111', 'Notificaciones', 1, 0, 0, 1);
    ensure_permission('07a733f3-3a97-42be-82d7-7aeb39366eca', '11111111-1111-1111-1111-111111111111', 'Usuarios', 1, 1, 1, 1);
    ensure_permission('7fb38b70-ac2f-4998-b64a-a769f27fdf7b', '11111111-1111-1111-1111-111111111111', 'Roles', 1, 1, 1, 1);
    ensure_permission('d1e3a202-1e6c-4a86-94c3-289de0ca7c21', '11111111-1111-1111-1111-111111111111', 'Reportes', 1, 0, 0, 0);

    -- Veterinario
    ensure_permission('0f5fbe54-b049-480d-8a54-1cc6e5bace30', '44444444-4444-4444-4444-444444444444', 'Clientes', 1, 0, 0, 0);
    ensure_permission('13b24e43-926a-4680-adee-0230ddb26c79', '44444444-4444-4444-4444-444444444444', 'Mascotas', 1, 0, 0, 0);
    ensure_permission('79093a43-37c7-414b-8795-f8cd6873eb7b', '44444444-4444-4444-4444-444444444444', 'Especies y Razas', 1, 0, 0, 0);
    ensure_permission('97dfa9c2-1cb2-4294-9f14-a6a226817f8c', '44444444-4444-4444-4444-444444444444', 'Especialidades', 1, 0, 0, 0);
    ensure_permission('adca595a-367f-45a5-8100-bd61f316bbc5', '44444444-4444-4444-4444-444444444444', 'Veterinarios', 1, 0, 0, 0);
    ensure_permission('017ae40b-1d25-4e20-984a-258e0e2f1fe5', '44444444-4444-4444-4444-444444444444', 'Citas', 1, 0, 1, 0);
    ensure_permission('1ab5c377-ad36-48db-bd3b-db02eecf2d64', '44444444-4444-4444-4444-444444444444', 'Historiales Clínicos', 1, 1, 1, 0);
    ensure_permission('80f53cfa-0ecf-42aa-a9d7-38731f00f599', '44444444-4444-4444-4444-444444444444', 'Servicios', 1, 0, 0, 0);
    ensure_permission('bc33442d-6bee-4b34-87b6-4e521551c8a7', '44444444-4444-4444-4444-444444444444', 'Estados de Cita', 1, 0, 0, 0);

    -- Recepcionista
    ensure_permission('76be45ca-8349-410a-ab5c-ce4825bef0e0', '55555555-5555-5555-5555-555555555555', 'Clientes', 1, 1, 1, 0);
    ensure_permission('4d7f8f54-0364-4afb-8049-121136dc5595', '55555555-5555-5555-5555-555555555555', 'Mascotas', 1, 1, 1, 0);
    ensure_permission('1955bd98-c522-4377-84d1-9b1f2fec7d7b', '55555555-5555-5555-5555-555555555555', 'Especies y Razas', 1, 0, 0, 0);
    ensure_permission('57d716b1-2c4e-4949-8cda-276d1d8d0cb4', '55555555-5555-5555-5555-555555555555', 'Especialidades', 1, 0, 0, 0);
    ensure_permission('9a6c1378-c3b5-414f-a7ce-49e1ed565a8d', '55555555-5555-5555-5555-555555555555', 'Veterinarios', 1, 0, 0, 0);
    ensure_permission('d0e6df49-4072-4159-9b1c-54fbb6866b57', '55555555-5555-5555-5555-555555555555', 'Citas', 1, 1, 1, 1);
    ensure_permission('2f85e7b7-1b66-4c71-8195-75dd77a5cf7c', '55555555-5555-5555-5555-555555555555', 'Historiales Clínicos', 1, 0, 0, 0);
    ensure_permission('3642d164-4871-4982-b679-27fe48cd3e38', '55555555-5555-5555-5555-555555555555', 'Servicios', 1, 0, 0, 0);
    ensure_permission('73869746-0660-4d14-b298-e11b28258231', '55555555-5555-5555-5555-555555555555', 'Estados de Cita', 1, 0, 0, 0);
    ensure_permission('352fc580-3c6f-4ab4-b491-8a535e21b0d6', '55555555-5555-5555-5555-555555555555', 'Cuentas y Pagos', 1, 1, 0, 0);

    -- Auxiliar
    ensure_permission('b7540f8f-7ac3-4479-a46e-b0efc34d588c', '66666666-6666-6666-6666-666666666666', 'Clientes', 1, 0, 0, 0);
    ensure_permission('72686e74-29b0-46f6-b4bf-63fd6430ada9', '66666666-6666-6666-6666-666666666666', 'Mascotas', 1, 0, 0, 0);
    ensure_permission('86aef2cc-1b8b-4dd3-bffa-3ca04e4b5fcd', '66666666-6666-6666-6666-666666666666', 'Especies y Razas', 1, 0, 0, 0);
    ensure_permission('f0088433-eb22-419c-979d-900114049b96', '66666666-6666-6666-6666-666666666666', 'Especialidades', 1, 0, 0, 0);
    ensure_permission('83b9375a-6dff-4ef7-b727-53e648364ba6', '66666666-6666-6666-6666-666666666666', 'Citas', 1, 0, 0, 0);
    ensure_permission('8fee0f45-2c62-4aef-aae8-63a59df078a6', '66666666-6666-6666-6666-666666666666', 'Historiales Clínicos', 1, 0, 0, 0);
    ensure_permission('137f09ee-ac41-4abe-aff8-ba1306281c33', '66666666-6666-6666-6666-666666666666', 'Servicios', 1, 0, 0, 0);
    ensure_permission('d8bdcce9-0696-4f40-9828-193708deb19c', '66666666-6666-6666-6666-666666666666', 'Estados de Cita', 1, 0, 0, 0);

    -- Cliente = sin permisos de plataforma web
    DELETE FROM ROLE_PERMISSIONS
    WHERE ROLE_ID IN (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'CLIENTE');
END;
/

COMMIT;

-- =============================================================================
-- Nivel 4: Usuarios de prueba por rol (contraseña: Password123!)
-- =============================================================================

DECLARE
    v_password_hash VARCHAR2(255) := '100000.uDRzSo2R4QKRRm5lFo9i4w==.j6rbqW/M2w062GPACpVAwwn4cBEni67eUHwlh8VIbN4=';
BEGIN
    -- SuperAdmin
    MERGE INTO USERS target
    USING (
        SELECT 'user-superadmin-001' AS ID,
               (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'SUPERADMIN') AS ROLE_ID,
               'SuperAdmin Sistema' AS FULL_NAME,
               'superadmin@huellitas.local' AS EMAIL,
               v_password_hash AS PASSWORD_HASH
        FROM DUAL
    ) source
    ON (target.USER_ID = source.ID)
    WHEN NOT MATCHED THEN
        INSERT (USER_ID, ROLE_ID, FULL_NAME, EMAIL, PASSWORD_HASH, CREATED_AT)
        VALUES (source.ID, source.ROLE_ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, SYSTIMESTAMP);

    -- Administrador
    MERGE INTO USERS target
    USING (
        SELECT 'user-admin-001' AS ID,
               (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'ADMINISTRADOR') AS ROLE_ID,
               'Administrador Sistema' AS FULL_NAME,
               'admin@huellitas.local' AS EMAIL,
               v_password_hash AS PASSWORD_HASH
        FROM DUAL
    ) source
    ON (target.USER_ID = source.ID)
    WHEN NOT MATCHED THEN
        INSERT (USER_ID, ROLE_ID, FULL_NAME, EMAIL, PASSWORD_HASH, CREATED_AT)
        VALUES (source.ID, source.ROLE_ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, SYSTIMESTAMP);

    -- Veterinario
    MERGE INTO USERS target
    USING (
        SELECT 'user-vet-001' AS ID,
               (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'VETERINARIO') AS ROLE_ID,
               'Dr. Carlos Veterinario' AS FULL_NAME,
               'vet.carlos@huellitas.local' AS EMAIL,
               v_password_hash AS PASSWORD_HASH
        FROM DUAL
    ) source
    ON (target.USER_ID = source.ID)
    WHEN NOT MATCHED THEN
        INSERT (USER_ID, ROLE_ID, FULL_NAME, EMAIL, PASSWORD_HASH, CREATED_AT)
        VALUES (source.ID, source.ROLE_ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, SYSTIMESTAMP);

    -- Recepcionista
    MERGE INTO USERS target
    USING (
        SELECT 'user-recep-001' AS ID,
               (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'RECEPCIONISTA') AS ROLE_ID,
               'María Recepcionista' AS FULL_NAME,
               'recep.maria@huellitas.local' AS EMAIL,
               v_password_hash AS PASSWORD_HASH
        FROM DUAL
    ) source
    ON (target.USER_ID = source.ID)
    WHEN NOT MATCHED THEN
        INSERT (USER_ID, ROLE_ID, FULL_NAME, EMAIL, PASSWORD_HASH, CREATED_AT)
        VALUES (source.ID, source.ROLE_ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, SYSTIMESTAMP);

    -- Auxiliar
    MERGE INTO USERS target
    USING (
        SELECT 'user-aux-001' AS ID,
               (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'AUXILIAR') AS ROLE_ID,
               'Juan Auxiliar' AS FULL_NAME,
               'aux.juan@huellitas.local' AS EMAIL,
               v_password_hash AS PASSWORD_HASH
        FROM DUAL
    ) source
    ON (target.USER_ID = source.ID)
    WHEN NOT MATCHED THEN
        INSERT (USER_ID, ROLE_ID, FULL_NAME, EMAIL, PASSWORD_HASH, CREATED_AT)
        VALUES (source.ID, source.ROLE_ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, SYSTIMESTAMP);

    -- Cliente
    MERGE INTO USERS target
    USING (
        SELECT 'user-client-001' AS ID,
               (SELECT ROLE_ID FROM ROLES WHERE UPPER(NAME) = 'CLIENTE') AS ROLE_ID,
               'Cliente Prueba' AS FULL_NAME,
               'cliente.prueba@huellitas.local' AS EMAIL,
               v_password_hash AS PASSWORD_HASH
        FROM DUAL
    ) source
    ON (target.USER_ID = source.ID)
    WHEN NOT MATCHED THEN
        INSERT (USER_ID, ROLE_ID, FULL_NAME, EMAIL, PASSWORD_HASH, CREATED_AT)
        VALUES (source.ID, source.ROLE_ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, SYSTIMESTAMP);
END;
/

COMMIT;

-- =============================================================================
-- Fin de inserciones
-- =============================================================================