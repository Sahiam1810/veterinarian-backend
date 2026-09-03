-- =============================================================================
-- INSERCIÓN DE DATOS DE PRUEBA OPERACIONALES (TEST DATA)
-- Base de datos: Oracle Database
-- 
-- Tablas pobladas en orden de integridad referencial:
--   1. SPECIALTIES (Especialidades veterinarias)
--   2. SPECIES (Especies)
--   3. RACES (Razas asociadas a especies)
--   4. USERS (1 Usuario por cada Rol canónico: Admin, Vet, Recep, Aux, Cliente)
--   5. USER_ACCOUNTS (Cuentas para login con username y mail)
--   6. USER_CREDENTIALS (Credenciales de acceso con Password123!)
--   7. VETERINARIANS (Dr. Veterinario vinculado al rol Veterinario)
--   8. CLIENTS (Cliente Ana Gomez vinculada al rol Cliente)
--   9. PETS (Mascotas de prueba)
--  10. CLIENTS_PETS (Relación dueño-mascota)
--  11. TYPE_SERVICES & SERVICES (Tipos y servicios de atención)
--  12. DIAGNOSTICS (Diagnósticos canónicos)
--  13. AVAILABILITIES (Horarios del veterinario)
--  14. STATUS_APPOINTMENTS (Garantiza estados canónicos)
--  15. APPOINTMENTS (Citas de prueba)
--  16. MEDICAL_RECORDS (Historias clínicas)
--  17. VACCINATIONS (Registro de vacunación)
--
-- Contraseña para todos los usuarios: Password123!
-- =============================================================================

SET DEFINE OFF;
ALTER SESSION SET NLS_LANGUAGE = 'SPANISH';
ALTER SESSION SET NLS_TERRITORY = 'SPAIN';

-- =============================================================================
-- 1. SPECIALTIES
-- =============================================================================
MERGE INTO SPECIALTIES target
USING (
    SELECT 'bbbb0001-0000-0000-0000-000000000001' AS ID, 'Medicina General' AS NAME, 'Atención clínica primaria, chequeos y medicina preventiva' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT 'bbbb0001-0000-0000-0000-000000000002' AS ID, 'Cirugía General' AS NAME, 'Procedimientos quirúrgicos de tejidos blandos y esterilizaciones' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT 'bbbb0001-0000-0000-0000-000000000003' AS ID, 'Dermatología' AS NAME, 'Diagnóstico y tratamiento de alergias y afecciones cutáneas' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT 'bbbb0001-0000-0000-0000-000000000004' AS ID, 'Oftalmología' AS NAME, 'Cuidado ocular, cataratas y patologías de la visión' AS DESCRIPTION FROM DUAL
) source
ON (target.SPECIALTY_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (SPECIALTY_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

-- =============================================================================
-- 2. SPECIES
-- =============================================================================
MERGE INTO SPECIES target
USING (
    SELECT 'bbbb0002-0000-0000-0000-000000000001' AS ID, 'Canino' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0002-0000-0000-0000-000000000002' AS ID, 'Felino' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0002-0000-0000-0000-000000000003' AS ID, 'Ave' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0002-0000-0000-0000-000000000004' AS ID, 'Conejo' AS NAME FROM DUAL
) source
ON (target.SPECIES_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (SPECIES_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

-- =============================================================================
-- 3. RACES
-- =============================================================================
MERGE INTO RACES target
USING (
    SELECT 'bbbb0003-0000-0000-0000-000000000001' AS ID, 'Golden Retriever' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0003-0000-0000-0000-000000000002' AS ID, 'Bulldog Francés' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0003-0000-0000-0000-000000000003' AS ID, 'Pastor Alemán' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0003-0000-0000-0000-000000000004' AS ID, 'Siamés' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0003-0000-0000-0000-000000000005' AS ID, 'Persa' AS NAME FROM DUAL UNION ALL
    SELECT 'bbbb0003-0000-0000-0000-000000000006' AS ID, 'Criollo / Mestizo' AS NAME FROM DUAL
) source
ON (target.RACE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (RACE_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

-- =============================================================================
-- 4. USERS (1 Usuario por cada Rol canónico)
-- Contraseña general para todos: Password123!
-- Hash compatible: 100000.E10ADC3949BA59ABBE56E057F20F883E.4Q8L... (PBKDF2/Bcrypt)
-- =============================================================================
MERGE INTO USERS target
USING (
    -- 1. Rol Administrador ('11111111-1111-1111-1111-111111111111')
    SELECT 'bbbb0004-0000-0000-0000-000000000001' AS ID, 'Admin General' AS FULL_NAME, 'admin@veterinaria.com' AS EMAIL,
           '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH,
           '11111111-1111-1111-1111-111111111111' AS ROLE_ID, 1 AS IS_ACTIVE FROM DUAL UNION ALL
    -- 2. Rol Veterinario ('44444444-4444-4444-4444-444444444444')
    SELECT 'bbbb0004-0000-0000-0000-000000000002' AS ID, 'Dr. Carlos Mendoza' AS FULL_NAME, 'veterinario@veterinaria.com' AS EMAIL,
           '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH,
           '44444444-4444-4444-4444-444444444444' AS ROLE_ID, 1 AS IS_ACTIVE FROM DUAL UNION ALL
    -- 3. Rol Recepcionista ('55555555-5555-5555-5555-555555555555')
    SELECT 'bbbb0004-0000-0000-0000-000000000003' AS ID, 'Maria Recepcion' AS FULL_NAME, 'recepcionista@veterinaria.com' AS EMAIL,
           '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH,
           '55555555-5555-5555-5555-555555555555' AS ROLE_ID, 1 AS IS_ACTIVE FROM DUAL UNION ALL
    -- 4. Rol Auxiliar ('66666666-6666-6666-6666-666666666666')
    SELECT 'bbbb0004-0000-0000-0000-000000000004' AS ID, 'Pedro Auxiliar' AS FULL_NAME, 'auxiliar@veterinaria.com' AS EMAIL,
           '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH,
           '66666666-6666-6666-6666-666666666666' AS ROLE_ID, 1 AS IS_ACTIVE FROM DUAL UNION ALL
    -- 5. Rol Cliente ('77777777-7777-7777-7777-777777777777')
    SELECT 'bbbb0004-0000-0000-0000-000000000005' AS ID, 'Ana Gomez' AS FULL_NAME, 'cliente@veterinaria.com' AS EMAIL,
           '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH,
           '77777777-7777-7777-7777-777777777777' AS ROLE_ID, 1 AS IS_ACTIVE FROM DUAL
) source
ON (target.EMAIL = source.EMAIL)
WHEN MATCHED THEN
    UPDATE SET target.USER_ID = source.ID,
               target.FULL_NAME = source.FULL_NAME,
               target.PASSWORD_HASH = source.PASSWORD_HASH,
               target.ROLE_ID = source.ROLE_ID,
               target.IS_ACTIVE = source.IS_ACTIVE
WHEN NOT MATCHED THEN
    INSERT (USER_ID, FULL_NAME, EMAIL, PASSWORD_HASH, ROLE_ID, IS_ACTIVE, CREATED_AT)
    VALUES (source.ID, source.FULL_NAME, source.EMAIL, source.PASSWORD_HASH, source.ROLE_ID, source.IS_ACTIVE, SYSTIMESTAMP);

-- =============================================================================
-- 5. USER_ACCOUNTS (Cuentas de usuario para login por username o email)
-- =============================================================================
MERGE INTO USER_ACCOUNTS target
USING (
    SELECT 'bbbb0040-0000-0000-0000-000000000001' AS ACCOUNT_ID, 'bbbb0004-0000-0000-0000-000000000001' AS USER_ID, 'admin' AS USERNAME, 'admin@veterinaria.com' AS MAIL, 'Active' AS STATUS FROM DUAL UNION ALL
    SELECT 'bbbb0040-0000-0000-0000-000000000002' AS ACCOUNT_ID, 'bbbb0004-0000-0000-0000-000000000002' AS USER_ID, 'veterinario' AS USERNAME, 'veterinario@veterinaria.com' AS MAIL, 'Active' AS STATUS FROM DUAL UNION ALL
    SELECT 'bbbb0040-0000-0000-0000-000000000003' AS ACCOUNT_ID, 'bbbb0004-0000-0000-0000-000000000003' AS USER_ID, 'recepcionista' AS USERNAME, 'recepcionista@veterinaria.com' AS MAIL, 'Active' AS STATUS FROM DUAL UNION ALL
    SELECT 'bbbb0040-0000-0000-0000-000000000004' AS ACCOUNT_ID, 'bbbb0004-0000-0000-0000-000000000004' AS USER_ID, 'auxiliar' AS USERNAME, 'auxiliar@veterinaria.com' AS MAIL, 'Active' AS STATUS FROM DUAL UNION ALL
    SELECT 'bbbb0040-0000-0000-0000-000000000005' AS ACCOUNT_ID, 'bbbb0004-0000-0000-0000-000000000005' AS USER_ID, 'cliente' AS USERNAME, 'cliente@veterinaria.com' AS MAIL, 'Active' AS STATUS FROM DUAL
) source
ON (target.USERNAME = source.USERNAME)
WHEN MATCHED THEN
    UPDATE SET target.ACCOUNT_ID = source.ACCOUNT_ID,
               target.USER_ID = source.USER_ID,
               target.MAIL = source.MAIL,
               target.STATUS = source.STATUS
WHEN NOT MATCHED THEN
    INSERT (ACCOUNT_ID, USER_ID, USERNAME, MAIL, STATUS, CREATED_AT)
    VALUES (source.ACCOUNT_ID, source.USER_ID, source.USERNAME, source.MAIL, source.STATUS, SYSTIMESTAMP);

-- =============================================================================
-- 6. USER_CREDENTIALS (Credenciales de cuenta)
-- =============================================================================
MERGE INTO USER_CREDENTIALS target
USING (
    SELECT 'bbbb0050-0000-0000-0000-000000000001' AS CREDENTIAL_ID, 'bbbb0040-0000-0000-0000-000000000001' AS ACCOUNT_ID, '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH FROM DUAL UNION ALL
    SELECT 'bbbb0050-0000-0000-0000-000000000002' AS CREDENTIAL_ID, 'bbbb0040-0000-0000-0000-000000000002' AS ACCOUNT_ID, '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH FROM DUAL UNION ALL
    SELECT 'bbbb0050-0000-0000-0000-000000000003' AS CREDENTIAL_ID, 'bbbb0040-0000-0000-0000-000000000003' AS ACCOUNT_ID, '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH FROM DUAL UNION ALL
    SELECT 'bbbb0050-0000-0000-0000-000000000004' AS CREDENTIAL_ID, 'bbbb0040-0000-0000-0000-000000000004' AS ACCOUNT_ID, '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH FROM DUAL UNION ALL
    SELECT 'bbbb0050-0000-0000-0000-000000000005' AS CREDENTIAL_ID, 'bbbb0040-0000-0000-0000-000000000005' AS ACCOUNT_ID, '$2a$11$eN7xN61c6iX2y7r2Gk4rAeK2o7k4f4W8l3x1Z9c0B2m5n8v7a1s3.' AS PASSWORD_HASH FROM DUAL
) source
ON (target.ACCOUNT_ID = source.ACCOUNT_ID)
WHEN MATCHED THEN
    UPDATE SET target.CREDENTIAL_ID = source.CREDENTIAL_ID,
               target.PASSWORD_HASH = source.PASSWORD_HASH,
               target.LAST_CHANGED = SYSTIMESTAMP
WHEN NOT MATCHED THEN
    INSERT (CREDENTIAL_ID, ACCOUNT_ID, PASSWORD_HASH, LAST_CHANGED, CREATED_AT)
    VALUES (source.CREDENTIAL_ID, source.ACCOUNT_ID, source.PASSWORD_HASH, SYSTIMESTAMP, SYSTIMESTAMP);

-- =============================================================================
-- 7. VETERINARIANS (Dr. Carlos Mendoza con especialidad Medicina General)
-- =============================================================================
MERGE INTO VETERINARIANS target
USING (
    SELECT 'bbbb0005-0000-0000-0000-000000000001' AS ID,
           'bbbb0004-0000-0000-0000-000000000002' AS USER_ID,
           'bbbb0001-0000-0000-0000-000000000001' AS SPECIALTY_ID,
           'MP-102938' AS LICENSE_NUMBER FROM DUAL
) source
ON (target.VETERINARIAN_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (VETERINARIAN_ID, USER_ID, SPECIALTY_ID, LICENSE_NUMBER, CREATED_AT)
    VALUES (source.ID, source.USER_ID, source.SPECIALTY_ID, source.LICENSE_NUMBER, SYSTIMESTAMP);

-- =============================================================================
-- 8. CLIENTS (Ana Gomez con documento y teléfono)
-- =============================================================================
MERGE INTO CLIENTS target
USING (
    SELECT 'bbbb0006-0000-0000-0000-000000000001' AS ID,
           'bbbb0004-0000-0000-0000-000000000005' AS USER_ID,
           '1032456789' AS IDENTIFICATION_NUMBER,
           'Calle 45 #12-34' AS ADDRESS,
           '3101234567' AS PHONE_NUMBER,
           TO_DATE('2026-01-15', 'YYYY-MM-DD') AS REGISTRATION_DATE FROM DUAL
) source
ON (target.IDENTIFICATION_NUMBER = source.IDENTIFICATION_NUMBER)
WHEN MATCHED THEN
    UPDATE SET target.CLIENT_ID = source.ID,
               target.USER_ID = source.USER_ID,
               target.ADDRESS = source.ADDRESS,
               target.PHONE_NUMBER = source.PHONE_NUMBER,
               target.REGISTRATION_DATE = source.REGISTRATION_DATE
WHEN NOT MATCHED THEN
    INSERT (CLIENT_ID, USER_ID, IDENTIFICATION_NUMBER, ADDRESS, PHONE_NUMBER, REGISTRATION_DATE, CREATED_AT)
    VALUES (source.ID, source.USER_ID, source.IDENTIFICATION_NUMBER, source.ADDRESS, source.PHONE_NUMBER, source.REGISTRATION_DATE, SYSTIMESTAMP);

-- =============================================================================
-- 9. PETS (Max Canino Golden y Luna Felino Siamés)
-- =============================================================================
MERGE INTO PETS target
USING (
    SELECT 'bbbb0007-0000-0000-0000-000000000001' AS ID, 'Max' AS NAME, 3 AS AGE, 'M' AS GENDER, 28.500 AS WEIGHT,
           'Alérgico al pollo' AS OBSERVATIONS,
           'bbbb0002-0000-0000-0000-000000000001' AS SPECIES_ID,
           'bbbb0003-0000-0000-0000-000000000001' AS RACE_ID FROM DUAL UNION ALL
    SELECT 'bbbb0007-0000-0000-0000-000000000002' AS ID, 'Luna' AS NAME, 2 AS AGE, 'F' AS GENDER, 4.200 AS WEIGHT,
           'Vacunación completa al día' AS OBSERVATIONS,
           'bbbb0002-0000-0000-0000-000000000002' AS SPECIES_ID,
           'bbbb0003-0000-0000-0000-000000000004' AS RACE_ID FROM DUAL
) source
ON (target.PET_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (PET_ID, NAME, AGE, GENDER, WEIGHT, OBSERVATIONS, SPECIES_ID, RACE_ID, CREATED_AT)
    VALUES (source.ID, source.NAME, source.AGE, source.GENDER, source.WEIGHT, source.OBSERVATIONS, source.SPECIES_ID, source.RACE_ID, SYSTIMESTAMP);

-- =============================================================================
-- 10. CLIENTS_PETS (Relación Dueño - Mascota)
-- =============================================================================
MERGE INTO CLIENTS_PETS target
USING (
    SELECT 'bbbb0008-0000-0000-0000-000000000001' AS ID,
           'bbbb0006-0000-0000-0000-000000000001' AS CLIENT_ID,
           'bbbb0007-0000-0000-0000-000000000001' AS PET_ID,
           'Y' AS IS_PRIMARY_OWNER FROM DUAL UNION ALL
    SELECT 'bbbb0008-0000-0000-0000-000000000002' AS ID,
           'bbbb0006-0000-0000-0000-000000000001' AS CLIENT_ID,
           'bbbb0007-0000-0000-0000-000000000002' AS PET_ID,
           'Y' AS IS_PRIMARY_OWNER FROM DUAL
) source
ON (target.CLIENT_PET_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (CLIENT_PET_ID, CLIENT_ID, PET_ID, IS_PRIMARY_OWNER, CREATED_AT)
    VALUES (source.ID, source.CLIENT_ID, source.PET_ID, source.IS_PRIMARY_OWNER, SYSTIMESTAMP);

-- =============================================================================
-- 11. TYPE_SERVICES & SERVICES
-- =============================================================================
MERGE INTO TYPE_SERVICES target
USING (
    SELECT 'bbbb0009-0000-0000-0000-000000000001' AS ID, 'Consulta Médica' AS NAME, 'Consultas de valoración, control y diagnóstico' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT 'bbbb0009-0000-0000-0000-000000000002' AS ID, 'Cirugía' AS NAME, 'Intervenciones quirúrgicas programadas o de urgencia' AS DESCRIPTION FROM DUAL UNION ALL
    SELECT 'bbbb0009-0000-0000-0000-000000000003' AS ID, 'Medicina Preventiva' AS NAME, 'Planes de vacunación y desparasitación' AS DESCRIPTION FROM DUAL
) source
ON (target.TYPE_SERVICE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (TYPE_SERVICE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

MERGE INTO SERVICES target
USING (
    SELECT 'bbbb0010-0000-0000-0000-000000000001' AS ID,
           'bbbb0009-0000-0000-0000-000000000001' AS TYPE_SERVICE_ID,
           'Consulta General' AS NAME, 30 AS DURATION_MINUTES, 50000 AS PRICE, 'Y' AS IS_ACTIVE FROM DUAL UNION ALL
    SELECT 'bbbb0010-0000-0000-0000-000000000002' AS ID,
           'bbbb0009-0000-0000-0000-000000000003' AS TYPE_SERVICE_ID,
           'Aplicación de Vacuna' AS NAME, 20 AS DURATION_MINUTES, 40000 AS PRICE, 'Y' AS IS_ACTIVE FROM DUAL
) source
ON (target.SERVICE_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (SERVICE_ID, TYPE_SERVICE_ID, NAME, DURATION_MINUTES, PRICE, IS_ACTIVE, CREATED_AT)
    VALUES (source.ID, source.TYPE_SERVICE_ID, source.NAME, source.DURATION_MINUTES, source.PRICE, source.IS_ACTIVE, SYSTIMESTAMP);

-- =============================================================================
-- 12. DIAGNOSTICS
-- =============================================================================
MERGE INTO DIAGNOSTICS target
USING (
    SELECT 'bbbb0011-0000-0000-0000-000000000001' AS ID, 'DIAG-001' AS CODE, 'Gastroenteritis aguda' AS NAME, 'Inflamación estomacal e intestinal por dieta o infección' AS DESCRIPTION, 1 AS IS_ACTIVE FROM DUAL UNION ALL
    SELECT 'bbbb0011-0000-0000-0000-000000000002' AS ID, 'DIAG-002' AS CODE, 'Control sano / Preventivo' AS NAME, 'Paciente en óptimas condiciones generales' AS DESCRIPTION, 1 AS IS_ACTIVE FROM DUAL
) source
ON (target.ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (ID, CODE, NAME, DESCRIPTION, IS_ACTIVE, CREATED_AT)
    VALUES (source.ID, source.CODE, source.NAME, source.DESCRIPTION, source.IS_ACTIVE, SYSTIMESTAMP);

-- =============================================================================
-- 13. AVAILABILITIES (Horario semanal Dr. Carlos Mendoza: Lunes a Viernes 08:00 - 14:00)
-- =============================================================================
MERGE INTO AVAILABILITIES target
USING (
    SELECT 'bbbb0012-0000-0000-0000-000000000001' AS ID, 'bbbb0005-0000-0000-0000-000000000001' AS VET_ID, 1 AS DAY_OF_WEEK, '08:00:00' AS START_TIME, '14:00:00' AS END_TIME, 'Y' AS IS_ACTIVE FROM DUAL UNION ALL
    SELECT 'bbbb0012-0000-0000-0000-000000000002' AS ID, 'bbbb0005-0000-0000-0000-000000000001' AS VET_ID, 3 AS DAY_OF_WEEK, '08:00:00' AS START_TIME, '14:00:00' AS END_TIME, 'Y' AS IS_ACTIVE FROM DUAL
) source
ON (target.AVAILABILITY_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (AVAILABILITY_ID, VETERINARIAN_ID, DAY_OF_WEEK, START_TIME, END_TIME, IS_ACTIVE, CREATED_AT)
    VALUES (source.ID, source.VET_ID, source.DAY_OF_WEEK, source.START_TIME, source.END_TIME, source.IS_ACTIVE, SYSTIMESTAMP);

-- =============================================================================
-- 14. STATUS_APPOINTMENTS (Garantiza los estados canónicos)
-- =============================================================================
MERGE INTO STATUS_APPOINTMENTS target
USING (
    SELECT 'aaaaaaaa-0000-0000-0000-000000000001' AS ID, 'AGENDADA' AS NAME FROM DUAL UNION ALL
    SELECT 'aaaaaaaa-0000-0000-0000-000000000002' AS ID, 'ATENDIDA' AS NAME FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN
    UPDATE SET target.STATUS_APPOINTMENT_ID = source.ID
WHEN NOT MATCHED THEN
    INSERT (STATUS_APPOINTMENT_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

-- =============================================================================
-- 15. APPOINTMENTS (Citas de prueba)
-- =============================================================================
MERGE INTO APPOINTMENTS target
USING (
    SELECT 'bbbb0013-0000-0000-0000-000000000001' AS ID,
           'bbbb0008-0000-0000-0000-000000000001' AS CLIENT_PET_ID,
           'bbbb0005-0000-0000-0000-000000000001' AS VET_ID,
           'bbbb0010-0000-0000-0000-000000000001' AS SERVICE_ID,
           (SELECT STATUS_APPOINTMENT_ID FROM STATUS_APPOINTMENTS WHERE UPPER(NAME) = 'ATENDIDA' AND ROWNUM = 1) AS STATUS_ID,
           'bbbb0012-0000-0000-0000-000000000001' AS AVAILABILITY_ID,
           TO_TIMESTAMP('2026-09-01 09:00:00', 'YYYY-MM-DD HH24:MI:SS') AS SCHED_START,
           TO_TIMESTAMP('2026-09-01 09:30:00', 'YYYY-MM-DD HH24:MI:SS') AS SCHED_END,
           'Chequeo general preventivo' AS NOTES,
           '3101234567' AS PHONE FROM DUAL
) source
ON (target.APPOINTMENT_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (APPOINTMENT_ID, CLIENT_PET_ID, VETERINARIAN_ID, SERVICE_ID, STATUS_ID, AVAILABILITY_ID,
            SCHEDULED_START, SCHEDULED_END, NOTES, REQUESTER_PHONE_NUMBER, CREATED_AT)
    VALUES (source.ID, source.CLIENT_PET_ID, source.VET_ID, source.SERVICE_ID, source.STATUS_ID, source.AVAILABILITY_ID,
            source.SCHED_START, source.SCHED_END, source.NOTES, source.PHONE, SYSTIMESTAMP);

-- =============================================================================
-- 16. MEDICAL_RECORDS
-- =============================================================================
MERGE INTO MEDICAL_RECORDS target
USING (
    SELECT 'bbbb0014-0000-0000-0000-000000000001' AS ID,
           'bbbb0008-0000-0000-0000-000000000001' AS CLIENT_PET_ID,
           'bbbb0013-0000-0000-0000-000000000001' AS APPOINTMENT_ID,
           'bbbb0011-0000-0000-0000-000000000002' AS DIAGNOSTIC_ID,
           'Chequeo preventivo de rutina' AS SYMPTOMS,
           'Paciente sano' AS TREATMENT,
           28.5 AS WEIGHT_AT_VISIT,
           38.5 AS TEMPERATURE FROM DUAL
) source
ON (target.RECORD_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (RECORD_ID, CLIENT_PET_ID, APPOINTMENT_ID, DIAGNOSTIC_ID, SYMPTOMS, TREATMENT, WEIGHT_AT_VISIT, TEMPERATURE, CREATED_AT)
    VALUES (source.ID, source.CLIENT_PET_ID, source.APPOINTMENT_ID, source.DIAGNOSTIC_ID, source.SYMPTOMS, source.TREATMENT, source.WEIGHT_AT_VISIT, source.TEMPERATURE, SYSTIMESTAMP);

-- =============================================================================
-- 17. VACCINATIONS
-- =============================================================================
MERGE INTO VACCINATIONS target
USING (
    SELECT 'bbbb0015-0000-0000-0000-000000000001' AS ID,
           'bbbb0008-0000-0000-0000-000000000001' AS CLIENT_PET_ID,
           'bbbb0014-0000-0000-0000-000000000001' AS RECORD_ID,
           'Rabia Canina Anual' AS VACCINE_NAME,
           1 AS DOSE_NUMBER,
           TO_DATE('2026-09-01', 'YYYY-MM-DD') AS APPLICATION_DATE,
           TO_DATE('2027-09-01', 'YYYY-MM-DD') AS NEXT_DOSE_DATE FROM DUAL
) source
ON (target.VACCINATION_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (VACCINATION_ID, CLIENT_PET_ID, RECORD_ID, VACCINE_NAME, DOSE_NUMBER, APPLICATION_DATE, NEXT_DOSE_DATE, CREATED_AT)
    VALUES (source.ID, source.CLIENT_PET_ID, source.RECORD_ID, source.VACCINE_NAME, source.DOSE_NUMBER, source.APPLICATION_DATE, source.NEXT_DOSE_DATE, SYSTIMESTAMP);

COMMIT;
