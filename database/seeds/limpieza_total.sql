-- =============================================================================
-- SCRIPT MAESTRO DE LIMPIEZA TOTAL (CLEANUP ALL SEEDS & TRANSACTIONAL DATA)
-- Base de datos: Oracle Database
--
-- Elimina todos los datos operacionales, transaccionales y catálogos en orden
-- estricto de integridad referencial (de tablas hijas a tablas padre).
--
-- NOTA: NO elimina tablas (no usa DROP TABLE), solo vacía las filas (DELETE FROM).
-- =============================================================================

SET DEFINE OFF;

-- =============================================================================
-- Nivel 1: Tablas hoja (nadie tiene llaves foráneas apuntando hacia ellas)
-- =============================================================================
DELETE FROM APPOINTMENT_ACTION_VERIFICATION_SESSIONS;
DELETE FROM VACCINATIONS;
DELETE FROM APPOINTMENT_STATUS_HISTORIES;
DELETE FROM ROLE_PERMISSIONS;
DELETE FROM USER_PERMISSIONS;
DELETE FROM NOTIFICATIONS;
DELETE FROM ACCOUNT_STATEMENTS;
DELETE FROM USER_CREDENTIALS;
DELETE FROM USER_TOKENS;

-- Hojas del subsistema de Telegram
DELETE FROM TELEGRAM_LINK_CODES;
DELETE FROM TELEGRAM_LINKING_SESSIONS;
DELETE FROM TELEGRAM_REGISTRATION_SESSIONS;
DELETE FROM TELEGRAM_CONVERSATION_LINKS;

-- Hojas del subsistema de Chat e IA
DELETE FROM CHAT_ATTACHMENTS;
DELETE FROM CHAT_AI_RUN_ERRORS;
DELETE FROM CHAT_AI_RUN_METRICS;
DELETE FROM CHAT_CONVERSATION_AI_SETTINGS;
DELETE FROM CHAT_CONVERSATION_ASSIGNMENTS;
DELETE FROM CHAT_ESCALATION_ASSIGNMENTS;
DELETE FROM CHAT_ESCALATION_RESOLUTION;
DELETE FROM CHAT_ESCALATION_STATUS_HISTORY;

-- =============================================================================
-- Nivel 2: Tablas dependientes de Citas, Usuarios y Conversaciones
-- =============================================================================
DELETE FROM MEDICAL_RECORDS;
DELETE FROM USER_ACCOUNTS;
DELETE FROM TELEGRAM_USER_LINKS;
DELETE FROM CHAT_AI_RUNS;
DELETE FROM CHAT_ESCALATIONS;

-- =============================================================================
-- Nivel 3: Citas y Mensajes de Chat
-- =============================================================================
DELETE FROM APPOINTMENTS;
DELETE FROM CHAT_MESSAGES;

-- =============================================================================
-- Nivel 4: Disponibilidades, Vínculos Cliente-Mascota y Participantes del Chat
-- =============================================================================
DELETE FROM AVAILABILITIES;
DELETE FROM CLIENTS_PETS;
DELETE FROM CHAT_PARTICIPANTS;

-- =============================================================================
-- Nivel 5: Entidades principales de negocio y Conversaciones
-- =============================================================================
DELETE FROM PETS;
DELETE FROM CLIENTS;
DELETE FROM VETERINARIANS;
DELETE FROM SERVICES;
DELETE FROM AGENT_HUMANS;
DELETE FROM CHAT_USER_PROFILES;
DELETE FROM CHAT_CONVERSATIONS;

-- =============================================================================
-- Nivel 6: Usuarios y Razas (dependen de Roles y Especies)
-- =============================================================================
DELETE FROM USERS;
DELETE FROM RACES;

-- =============================================================================
-- Nivel 7: Catálogos raíz (sin dependencias externas)
-- =============================================================================
DELETE FROM SPECIALTIES;
DELETE FROM TYPE_SERVICES;
DELETE FROM DIAGNOSTICS;
DELETE FROM SPECIES;
DELETE FROM STATUS_APPOINTMENTS;
DELETE FROM CONVERSATIONS_STATUSES;
DELETE FROM SENDER_TYPES;
DELETE FROM MESSAGE_TYPES;
DELETE FROM PRIORITY;
DELETE FROM ESCALATIONS_STATUSES;
DELETE FROM AI_RUNS_STATUSES;
DELETE FROM MODULES;
DELETE FROM ROLES;

COMMIT;

-- =============================================================================
-- Fin de limpieza total
-- =============================================================================