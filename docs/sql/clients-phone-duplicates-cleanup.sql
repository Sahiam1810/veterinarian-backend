-- Pre-migración UX_CLIENTS_PHONE_NUMBER (tarea 2.1).
-- Ejecutar en el esquema real (p. ej. VET_APP) ANTES de aplicar la migración EF.
-- Objetivo: detectar y limpiar PHONE_NUMBER duplicados (ya normalizados a solo dígitos).

-- 1) Listar teléfonos no nulos con más de un CLIENT_ID.
SELECT
    PHONE_NUMBER,
    COUNT(*) AS CLIENT_COUNT
FROM CLIENTS
WHERE PHONE_NUMBER IS NOT NULL
GROUP BY PHONE_NUMBER
HAVING COUNT(*) > 1
ORDER BY CLIENT_COUNT DESC, PHONE_NUMBER;

-- 2) Detalle de filas duplicadas (revisar antes de borrar/actualizar).
SELECT
    c.CLIENT_ID,
    c.USER_ID,
    c.IDENTIFICATION_NUMBER,
    c.PHONE_NUMBER,
    c.CREATED_AT,
    c.UPDATED_AT
FROM CLIENTS c
WHERE c.PHONE_NUMBER IN (
    SELECT PHONE_NUMBER
    FROM CLIENTS
    WHERE PHONE_NUMBER IS NOT NULL
    GROUP BY PHONE_NUMBER
    HAVING COUNT(*) > 1
)
ORDER BY c.PHONE_NUMBER, c.CREATED_AT;

-- 3) Ejemplo de limpieza (elegir un ganador por teléfono y anular el resto).
-- AJUSTAR criterios de negocio antes de ejecutar; no correr a ciegas.
-- UPDATE CLIENTS
-- SET PHONE_NUMBER = NULL,
--     UPDATED_AT = SYSTIMESTAMP
-- WHERE CLIENT_ID IN (
--     SELECT CLIENT_ID FROM (
--         SELECT
--             CLIENT_ID,
--             ROW_NUMBER() OVER (
--                 PARTITION BY PHONE_NUMBER
--                 ORDER BY CREATED_AT ASC, CLIENT_ID ASC
--             ) AS RN
--         FROM CLIENTS
--         WHERE PHONE_NUMBER IS NOT NULL
--     )
--     WHERE RN > 1
-- );

-- 4) Verificar que no quedan duplicados no nulos.
SELECT
    PHONE_NUMBER,
    COUNT(*) AS CLIENT_COUNT
FROM CLIENTS
WHERE PHONE_NUMBER IS NOT NULL
GROUP BY PHONE_NUMBER
HAVING COUNT(*) > 1;

-- Si la consulta 4 no devuelve filas, aplicar la migración AddClientsPhoneNumberUniqueIndex.
