-- =============================================================================
-- LIMPIEZA DE DATOS (EXCLUSIVA PARA TABLAS DEL SEED DE INSERCIÓN)
-- Base de datos: Oracle Database
-- 
-- IMPORTANTE:
-- 1. NO elimina ninguna tabla (solo borra las filas con DELETE FROM). La estructura
--    y las tablas permanecen intactas.
-- 2. ÚNICAMENTE vacía las tablas que se pueblan en "insercion_datos.sql" y las tablas
--    hijas de auditoría/credenciales que dependen de ellas.
-- 3. NO toca: MODULES, ROLES, ROLE_PERMISSIONS, STATUS_APPOINTMENTS, CHAT_CONVERSATIONS, etc.
-- =============================================================================

SET DEFINE OFF;

-- 1. Tablas hijas dependientes de Citas y Mascotas
DELETE FROM APPOINTMENT_ACTION_VERIFICATION_SESSIONS;
DELETE FROM VACCINATIONS;
DELETE FROM MEDICAL_RECORDS;
DELETE FROM APPOINTMENT_STATUS_HISTORIES;
DELETE FROM APPOINTMENTS;

-- 2. Disponibilidad y Diagnósticos
DELETE FROM AVAILABILITIES;
DELETE FROM DIAGNOSTICS;

-- 3. Servicios
DELETE FROM SERVICES;
DELETE FROM TYPE_SERVICES;

-- 4. Dueños y Mascotas
DELETE FROM CLIENTS_PETS;
DELETE FROM PETS;
DELETE FROM CLIENTS;
DELETE FROM RACES;
DELETE FROM SPECIES;

-- 5. Veterinarios y Especialidades
DELETE FROM VETERINARIANS;
DELETE FROM SPECIALTIES;

-- 6. Usuarios, Cuentas y Credenciales
DELETE FROM USER_TOKENS;
DELETE FROM USER_CREDENTIALS;
DELETE FROM USER_ACCOUNTS;
DELETE FROM USERS;

COMMIT;
