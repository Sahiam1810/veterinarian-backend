-- =============================================================================
-- READ-ONLY AUDIT SCRIPT.
-- DO NOT run cleanup or DELETE/UPDATE statements from this file.
-- Do not run against Production without review and backup.
-- =============================================================================
-- Ticket 3 P1 — Auditoría de clientes huérfanos / placeholders de
-- AddClientProfileColumns (FULL_NAME 'Cliente pendiente' + EMAIL pending-*).
--
-- Solo SELECT. Ejecutar dos veces es seguro (idempotente / no muta datos).
-- No inventar ni reemplazar teléfonos. No anular PHONE_NUMBER mientras sea NOT NULL.
-- Consultas activas: únicamente CLIENTS + diccionario USER_* (sin tablas hijas).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Preflight / schema
-- -----------------------------------------------------------------------------

-- 1.1 Metadata de CLIENTS.PHONE_NUMBER (esquema actual del usuario).
SELECT
    utc.COLUMN_NAME,
    utc.DATA_TYPE,
    utc.DATA_LENGTH,
    utc.CHAR_LENGTH,
    utc.NULLABLE,
    utc.DATA_DEFAULT
FROM USER_TAB_COLUMNS utc
WHERE utc.TABLE_NAME = 'CLIENTS'
  AND utc.COLUMN_NAME = 'PHONE_NUMBER';

-- 1.2 Columnas de CLIENTS relevantes para marcadores y timestamps.
SELECT
    utc.COLUMN_NAME,
    utc.DATA_TYPE,
    utc.NULLABLE
FROM USER_TAB_COLUMNS utc
WHERE utc.TABLE_NAME = 'CLIENTS'
  AND utc.COLUMN_NAME IN (
      'CLIENT_ID',
      'FULL_NAME',
      'EMAIL',
      'PHONE_NUMBER',
      'IDENTIFICATION_NUMBER',
      'CREATED_AT',
      'UPDATED_AT',
      'IS_ACTIVE')
ORDER BY utc.COLUMN_ID;

-- 1.3 Índices relacionados con teléfono / email en CLIENTS.
SELECT
    ui.INDEX_NAME,
    ui.UNIQUENESS,
    uic.COLUMN_NAME,
    uic.COLUMN_POSITION
FROM USER_INDEXES ui
JOIN USER_IND_COLUMNS uic
  ON uic.INDEX_NAME = ui.INDEX_NAME
WHERE ui.TABLE_NAME = 'CLIENTS'
  AND (
      ui.INDEX_NAME = 'UX_CLIENTS_PHONE_NUMBER'
      OR uic.COLUMN_NAME IN ('PHONE_NUMBER', 'EMAIL', 'IDENTIFICATION_NUMBER')
  )
ORDER BY ui.INDEX_NAME, uic.COLUMN_POSITION;

-- 1.4 Constraints de CLIENTS (PK / unique / check) vía diccionario.
SELECT
    uc.CONSTRAINT_NAME,
    uc.CONSTRAINT_TYPE,
    ucc.COLUMN_NAME,
    ucc.POSITION
FROM USER_CONSTRAINTS uc
LEFT JOIN USER_CONS_COLUMNS ucc
  ON ucc.CONSTRAINT_NAME = uc.CONSTRAINT_NAME
 AND ucc.OWNER = uc.OWNER
WHERE uc.TABLE_NAME = 'CLIENTS'
  AND uc.CONSTRAINT_TYPE IN ('P', 'U', 'C')
ORDER BY uc.CONSTRAINT_TYPE, uc.CONSTRAINT_NAME, ucc.POSITION;

-- -----------------------------------------------------------------------------
-- 2. Candidates with trustworthy markers
--    Ambos marcadores (nombre + email). NO usar “teléfono parece GUID” solo.
-- -----------------------------------------------------------------------------

-- 2.1 Conteo de candidatos (marcadores inequívocos de AddClientProfileColumns).
SELECT COUNT(*) AS CANDIDATE_COUNT
FROM CLIENTS c
WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
  AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local';

-- 2.2 Muestra limitada (máx. 50). PII enmascarada: no listar email/teléfono completos.
SELECT *
FROM (
    SELECT
        c.CLIENT_ID,
        c.FULL_NAME,
        CASE
            WHEN c.EMAIL IS NULL THEN NULL
            WHEN INSTR(c.EMAIL, '@') > 1 THEN
                SUBSTR(c.EMAIL, 1, 1)
                || '***@'
                || SUBSTR(c.EMAIL, INSTR(c.EMAIL, '@') + 1)
            ELSE '***'
        END AS EMAIL_MASKED,
        CASE
            WHEN c.PHONE_NUMBER IS NULL THEN NULL
            WHEN LENGTH(c.PHONE_NUMBER) <= 4 THEN '****'
            ELSE LPAD('*', LENGTH(c.PHONE_NUMBER) - 4, '*')
                 || SUBSTR(c.PHONE_NUMBER, -4)
        END AS PHONE_MASKED,
        c.CREATED_AT,
        c.UPDATED_AT,
        c.IS_ACTIVE
    FROM CLIENTS c
    WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
      AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
    ORDER BY c.CREATED_AT NULLS LAST, c.CLIENT_ID
)
WHERE ROWNUM <= 50;

-- -----------------------------------------------------------------------------
-- 3. Dependency inventory (diccionario only — safe if no child tables exist)
-- -----------------------------------------------------------------------------

-- 3.1 Existencia en el esquema actual de tablas conocidas del modelo (metadata).
--     No lee filas de esas tablas; solo USER_TABLES.
SELECT TABLE_NAME
FROM USER_TABLES
WHERE TABLE_NAME IN (
    'CLIENTS',
    'CLIENTS_PETS',
    'TELEGRAM_USER_LINKS',
    'CHAT_PARTICIPANTS',
    'NOTIFICATIONS',
    'APPOINTMENTS')
ORDER BY TABLE_NAME;

-- 3.2 FKs hacia CLIENTS (tabla hija, constraint, columnas, padre).
--     Funciona aunque no haya tablas hijas: el resultado queda vacío.
SELECT
    uc.TABLE_NAME AS CHILD_TABLE,
    uc.CONSTRAINT_NAME AS FK_CONSTRAINT,
    ucc.COLUMN_NAME AS CHILD_COLUMN,
    r.TABLE_NAME AS PARENT_TABLE,
    rucc.COLUMN_NAME AS PARENT_COLUMN
FROM USER_CONSTRAINTS uc
JOIN USER_CONS_COLUMNS ucc
  ON ucc.CONSTRAINT_NAME = uc.CONSTRAINT_NAME
 AND ucc.OWNER = uc.OWNER
JOIN USER_CONSTRAINTS r
  ON r.CONSTRAINT_NAME = uc.R_CONSTRAINT_NAME
 AND r.OWNER = NVL(uc.R_OWNER, uc.OWNER)
JOIN USER_CONS_COLUMNS rucc
  ON rucc.CONSTRAINT_NAME = r.CONSTRAINT_NAME
 AND rucc.OWNER = r.OWNER
 AND rucc.POSITION = ucc.POSITION
WHERE uc.CONSTRAINT_TYPE = 'R'
  AND r.TABLE_NAME = 'CLIENTS'
ORDER BY uc.TABLE_NAME, uc.CONSTRAINT_NAME, ucc.POSITION;

-- -----------------------------------------------------------------------------
-- MANUAL FOLLOW-UP TEMPLATE — DO NOT EXECUTE UNTIL TABLE EXISTENCE IS CONFIRMED
-- -----------------------------------------------------------------------------
-- Tras confirmar en 3.1 / 3.2 que la tabla hija existe, puedes copiar UNA plantilla
-- a una hoja aparte. No forman parte de la ejecución del script. No mutan datos.
-- No eliminar ni actualizar clientes sin backup y revisión humana.
-- No usar GUID/placeholders como teléfonos.
-- No SET PHONE_NUMBER = NULL mientras la columna sea NOT NULL (ver 1.1).
--
-- -- Plantilla A: conteo en CLIENTS_PETS (solo si 3.1 lista CLIENTS_PETS)
-- -- SELECT 'CLIENTS_PETS' AS DEP_TABLE, COUNT(*) AS ROW_COUNT
-- -- FROM CLIENTS_PETS cp
-- -- WHERE cp.CLIENT_ID IN (
-- --     SELECT c.CLIENT_ID FROM CLIENTS c
-- --     WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
-- --       AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
-- -- );
--
-- -- Plantilla B: TELEGRAM_USER_LINKS (solo si 3.1 lista TELEGRAM_USER_LINKS)
-- -- SELECT 'TELEGRAM_USER_LINKS' AS DEP_TABLE, COUNT(*) AS ROW_COUNT
-- -- FROM TELEGRAM_USER_LINKS t
-- -- WHERE t.CLIENT_ID IN (
-- --     SELECT c.CLIENT_ID FROM CLIENTS c
-- --     WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
-- --       AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
-- -- );
--
-- -- Plantilla C: CHAT_PARTICIPANTS (solo si 3.1 lista CHAT_PARTICIPANTS)
-- -- SELECT 'CHAT_PARTICIPANTS' AS DEP_TABLE, COUNT(*) AS ROW_COUNT
-- -- FROM CHAT_PARTICIPANTS p
-- -- WHERE p.CLIENT_ID IN (
-- --     SELECT c.CLIENT_ID FROM CLIENTS c
-- --     WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
-- --       AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
-- -- );
--
-- -- Plantilla D: NOTIFICATIONS (solo si 3.1 lista NOTIFICATIONS)
-- -- SELECT 'NOTIFICATIONS' AS DEP_TABLE, COUNT(*) AS ROW_COUNT
-- -- FROM NOTIFICATIONS n
-- -- WHERE n.CLIENT_ID IN (
-- --     SELECT c.CLIENT_ID FROM CLIENTS c
-- --     WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
-- --       AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
-- -- );
--
-- -- Plantilla E: APPOINTMENTS vía CLIENTS_PETS (solo si ambas existen en 3.1)
-- -- SELECT 'APPOINTMENTS_VIA_CLIENTS_PETS' AS DEP_TABLE, COUNT(*) AS ROW_COUNT
-- -- FROM APPOINTMENTS a
-- -- WHERE a.CLIENT_PET_ID IN (
-- --     SELECT cp.CLIENT_PET_ID FROM CLIENTS_PETS cp
-- --     WHERE cp.CLIENT_ID IN (
-- --         SELECT c.CLIENT_ID FROM CLIENTS c
-- --         WHERE UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
-- --           AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
-- --     )
-- -- );
--
-- -- Plantilla F: links Telegram activos duplicados (solo si TELEGRAM_USER_LINKS existe)
-- -- SELECT t.TELEGRAM_USER_ID, COUNT(*) AS LINK_COUNT
-- -- FROM TELEGRAM_USER_LINKS t
-- -- WHERE t.UNLINKED_AT IS NULL
-- -- GROUP BY t.TELEGRAM_USER_ID
-- -- HAVING COUNT(*) > 1
-- -- ORDER BY LINK_COUNT DESC;
-- --
-- -- SELECT t.CLIENT_ID, COUNT(*) AS ACTIVE_LINK_COUNT
-- -- FROM TELEGRAM_USER_LINKS t
-- -- WHERE t.UNLINKED_AT IS NULL
-- -- GROUP BY t.CLIENT_ID
-- -- HAVING COUNT(*) > 1
-- -- ORDER BY ACTIVE_LINK_COUNT DESC;
-- -----------------------------------------------------------------------------

-- -----------------------------------------------------------------------------
-- 4. Validation checks (CLIENTS only)
-- -----------------------------------------------------------------------------

-- 4.1 Duplicados de teléfono (columna actual NOT NULL; se agrupa igual).
SELECT
    CASE
        WHEN LENGTH(c.PHONE_NUMBER) <= 4 THEN '****'
        ELSE LPAD('*', LENGTH(c.PHONE_NUMBER) - 4, '*')
             || SUBSTR(c.PHONE_NUMBER, -4)
    END AS PHONE_MASKED,
    COUNT(*) AS CLIENT_COUNT
FROM CLIENTS c
GROUP BY c.PHONE_NUMBER
HAVING COUNT(*) > 1
ORDER BY CLIENT_COUNT DESC;

-- 4.2 Conteo del placeholder histórico conocido '3000000000'.
SELECT COUNT(*) AS PLACEHOLDER_PHONE_3000000000_COUNT
FROM CLIENTS c
WHERE c.PHONE_NUMBER = '3000000000';

-- 4.3 Conteo de correos pending-*@huellitas.local (con o sin nombre pendiente).
SELECT COUNT(*) AS PENDING_EMAIL_COUNT
FROM CLIENTS c
WHERE LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local';

-- 4.4 Candidatos con ambos marcadores vs solo email pending (sin asumir GUID-phone).
SELECT
    SUM(CASE
            WHEN UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
             AND LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
            THEN 1 ELSE 0
        END) AS BOTH_MARKERS_COUNT,
    SUM(CASE
            WHEN LOWER(TRIM(c.EMAIL)) LIKE 'pending-%@huellitas.local'
             AND UPPER(TRIM(NVL(c.FULL_NAME, ' '))) <> UPPER('Cliente pendiente')
            THEN 1 ELSE 0
        END) AS PENDING_EMAIL_ONLY_COUNT,
    SUM(CASE
            WHEN UPPER(TRIM(c.FULL_NAME)) = UPPER('Cliente pendiente')
             AND LOWER(TRIM(NVL(c.EMAIL, ' '))) NOT LIKE 'pending-%@huellitas.local'
            THEN 1 ELSE 0
        END) AS PENDING_NAME_ONLY_COUNT
FROM CLIENTS c;

-- 4.5 Recordatorio: clientes reales NO se identifican solo por patrón de teléfono/GUID.
--     Cuenta filas con placeholder fijo exacto fuera del par de marcadores.
SELECT COUNT(*) AS EXACT_PLACEHOLDER_PHONE_ONLY
FROM CLIENTS c
WHERE c.PHONE_NUMBER = '3000000000'
  AND (
      UPPER(TRIM(NVL(c.FULL_NAME, ' '))) <> UPPER('Cliente pendiente')
      OR LOWER(TRIM(NVL(c.EMAIL, ' '))) NOT LIKE 'pending-%@huellitas.local'
  );

-- -----------------------------------------------------------------------------
-- 5. Manual remediation guidance — COMMENTS ONLY
-- -----------------------------------------------------------------------------
-- NO ejecutar desde este archivo:
--   DELETE / UPDATE / INSERT / MERGE / ALTER / DROP / TRUNCATE /
--   COMMIT / ROLLBACK / EXECUTE IMMEDIATE / BEGIN / DECLARE / CREATE /
--   GRANT / REVOKE
--
-- - No hacer UPDATE ... SET PHONE_NUMBER = NULL mientras CLIENTS.PHONE_NUMBER
--   sea NOT NULL (ver sección 1.1). Eso fallaría o requeriría decisión de
--   producto/esquema (dependencia T6 / nulabilidad).
-- - Solo se podría eliminar un candidato DESPUÉS de revisión humana, backup y
--   confirmación de dependencias (usar inventario 3.2 + plantillas manuales
--   solo tras confirmar tablas en 3.1).
-- - No inventar ni reemplazar teléfonos (ni GUID, ni fragmentos, ni 3000000000
--   “nuevos”, ni secuencias artificiales) para “arreglar” filas.
-- - Reejecutar las consultas activas de las secciones 1–4 es seguro: solo lectura.
-- - Si aparece el generador externo GUID→teléfono, ampliar marcadores solo con
--   evidencia inequívoca; no usar “parece GUID” como único criterio.
-- =============================================================================
