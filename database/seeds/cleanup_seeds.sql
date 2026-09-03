-- =============================================================================
-- LIMPIEZA DE DATOS DE SEEDS (database/seeds)
-- Base de datos: Oracle Database
--
-- Vacía las tablas pobladas por roles_seed.sql, modules_seed.sql,
-- role_permissions_seed.sql, status_appointments_seed.sql y
-- chat_conversation_catalogs_seed.sql (ROLES, MODULES, ROLE_PERMISSIONS,
-- STATUS_APPOINTMENTS, CONVERSATIONS_STATUSES, SENDER_TYPES), más TODA fila
-- que dependa de ellas por llave foránea (directa o transitivamente), para
-- poder re-ejecutar los seeds sin choques de duplicados/PK.
--
-- Ejecutar este script ANTES de volver a correr los *_seed.sql. No toca
-- catálogos ajenos a estos seeds (SPECIALTIES, SPECIES, RACES, PETS,
-- SERVICES, TYPE_SERVICES, DIAGNOSTICS, PRIORITY, AI_MODELS,
-- PROVIDER_MODELS_AI, MESSAGE_TYPES, AI_RUNS_STATUSES, ESCALATIONS_STATUSES),
-- ya que esos no son sembrados por los scripts de esta carpeta.
--
-- ADVERTENCIA: al arrastrar la dependencia transitiva de ROLES (USERS.ROLE_ID
-- es obligatorio) y de STATUS_APPOINTMENTS/CONVERSATIONS_STATUSES/SENDER_TYPES,
-- este script termina vaciando prácticamente todos los datos operacionales
-- (usuarios, citas, historia clínica, chat, telegram, cuentas). Úsalo solo en
-- entornos de desarrollo/prueba.
--
-- El orden respeta las dependencias de llave foránea: primero las tablas hoja
-- (nadie depende de ellas) y al final los 5 catálogos raíz de estos seeds.
-- =============================================================================

SET DEFINE OFF;

-- =============================================================================
-- Nivel 1: tablas hoja (ninguna otra tabla tiene FK hacia ellas)
-- =============================================================================
DELETE FROM ROLE_PERMISSIONS;
DELETE FROM USER_PERMISSIONS;
DELETE FROM NOTIFICATIONS;
DELETE FROM USER_CREDENTIALS;
DELETE FROM USER_TOKENS;
DELETE FROM ACCOUNT_STATEMENTS;
DELETE FROM VACCINATIONS;
DELETE FROM APPOINTMENT_STATUS_HISTORIES;
DELETE FROM CHAT_AI_RUN_ERRORS;
DELETE FROM CHAT_AI_RUN_METRICS;
DELETE FROM CHAT_CONVERSATION_AI_SETTINGS;
DELETE FROM CHAT_CONVERSATION_ASSIGNMENTS;
DELETE FROM CHAT_ESCALATION_ASSIGNMENTS;
DELETE FROM CHAT_ESCALATION_RESOLUTION;
DELETE FROM CHAT_ESCALATION_STATUS_HISTORY;
DELETE FROM CHAT_ATTACHMENTS;
DELETE FROM TELEGRAM_LINK_CODES;
DELETE FROM TELEGRAM_LINKING_SESSIONS;
DELETE FROM TELEGRAM_REGISTRATION_SESSIONS;
DELETE FROM TELEGRAM_CONVERSATION_LINKS;

-- =============================================================================
-- Nivel 2
-- =============================================================================
DELETE FROM TELEGRAM_USER_LINKS;
DELETE FROM USER_ACCOUNTS;
DELETE FROM MEDICAL_RECORDS;
DELETE FROM CHAT_AI_RUNS;
DELETE FROM CHAT_ESCALATIONS;

-- =============================================================================
-- Nivel 3
-- =============================================================================
DELETE FROM APPOINTMENTS;
DELETE FROM CHAT_MESSAGES;

-- =============================================================================
-- Nivel 4
-- =============================================================================
DELETE FROM CLIENTS_PETS;
DELETE FROM AVAILABILITIES;
DELETE FROM CHAT_PARTICIPANTS;

-- =============================================================================
-- Nivel 5
-- =============================================================================
DELETE FROM CLIENTS;
DELETE FROM VETERINARIANS;
DELETE FROM AGENT_HUMANS;
DELETE FROM CHAT_USER_PROFILES;
DELETE FROM CHAT_CONVERSATIONS;

-- =============================================================================
-- Nivel 6
-- =============================================================================
DELETE FROM USERS;

-- =============================================================================
-- Nivel 7: catálogos raíz sembrados por roles_seed.sql, modules_seed.sql,
-- status_appointments_seed.sql y chat_conversation_catalogs_seed.sql
-- =============================================================================
DELETE FROM ROLES;
DELETE FROM MODULES;
DELETE FROM STATUS_APPOINTMENTS;
DELETE FROM CONVERSATIONS_STATUSES;
DELETE FROM SENDER_TYPES;

COMMIT;
