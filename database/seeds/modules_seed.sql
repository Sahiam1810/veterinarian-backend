-- Catálogo de módulos del sistema (tabla MODULES).
-- Este script se ejecuta directamente contra la base de datos Oracle
-- (SQL*Plus, SQL Developer o DBeaver), fuera de la aplicación .NET.
--
-- Configuración de Encoding para SQL*Plus en Windows UTF-8:
SET DEFINE OFF;
ALTER SESSION SET NLS_LANGUAGE = 'SPANISH';
ALTER SESSION SET NLS_TERRITORY = 'SPAIN';

--
-- Requisitos antes de correr esto:
--   1. Las migraciones que crean la tabla MODULES deben estar aplicadas.
--
-- Es idempotente: usa MERGE para no duplicar filas si se ejecuta más de una vez.
-- Los MODULE_ID son fijos para que el entorno de cada integrante sea consistente.
--
-- Ejecutar ANTES de role_permissions_seed.sql, ya que ese script referencia
-- los módulos por NAME con subquery.

-- 1. Clientes
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000001' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Clientes', 'Gestión de clientes (dueños de mascotas)', SYSTIMESTAMP);

-- 2. Mascotas
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000002' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Mascotas', 'Gestión de mascotas registradas', SYSTIMESTAMP);

-- 3. Especies y Razas
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000003' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Especies y Razas', 'Catálogo de especies y razas de mascotas', SYSTIMESTAMP);

-- 4. Especialidades
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000004' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Especialidades', 'Catálogo de especialidades veterinarias', SYSTIMESTAMP);

-- 5. Veterinarios
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000005' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Veterinarios', 'Gestión de veterinarios del sistema', SYSTIMESTAMP);

-- 6. Citas
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000006' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Citas', 'Gestión de citas médicas veterinarias', SYSTIMESTAMP);

-- 7. Historiales Clínicos
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000007' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Historiales Clínicos', 'Historias médicas, vacunas y diagnósticos', SYSTIMESTAMP);

-- 8. Servicios
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000008' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Servicios', 'Catálogo de servicios y tipos de servicio', SYSTIMESTAMP);

-- 9. Estados de Cita
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000009' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Estados de Cita', 'Catálogo de estados posibles de una cita', SYSTIMESTAMP);

-- 10. Cuentas y Pagos
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000010' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Cuentas y Pagos', 'Estados de cuenta y pagos de clientes', SYSTIMESTAMP);

-- 11. Notificaciones
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000011' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Notificaciones', 'Gestión de notificaciones del sistema', SYSTIMESTAMP);

-- 12. Usuarios
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000012' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Usuarios', 'Gestión de usuarios, cuentas y credenciales', SYSTIMESTAMP);

-- 13. Roles
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000013' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Roles', 'Gestión de roles del sistema', SYSTIMESTAMP);

-- 14. Disponibilidades
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000014' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Disponibilidades', 'Gestión de disponibilidades de veterinarios', SYSTIMESTAMP);

-- 15. Relación Clientes-Mascotas
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000015' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Relación Clientes-Mascotas', 'Asociaciones entre clientes y mascotas', SYSTIMESTAMP);

-- 16. Permisos
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000016' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Permisos', 'Gestión de permisos por rol y por usuario', SYSTIMESTAMP);

-- 17. Chat
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000017' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Chat'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Chat', 'Gestión de conversaciones, participantes y mensajes', SYSTIMESTAMP);

-- 18. Escalamientos
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000018' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Escalamientos'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Escalamientos', 'Gestión y seguimiento de conversaciones escaladas', SYSTIMESTAMP);

-- 19. IA y Agente
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000019' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('IA y Agente'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'IA y Agente', 'Ejecuciones, modelos y configuración del agente conversacional', SYSTIMESTAMP);

-- 20. Catálogos del Chat
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000020' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Catálogos del Chat'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Catálogos del Chat', 'Estados, tipos y prioridades del subsistema conversacional', SYSTIMESTAMP);

COMMIT;
