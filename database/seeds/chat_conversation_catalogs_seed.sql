-- Catálogos mínimos para crear conversaciones desde el gateway del agente.
-- Ejecute este script después de aplicar las migraciones que crean
-- CONVERSATIONS_STATUSES y SENDER_TYPES.
-- Es idempotente: Oracle no inserta nuevamente los identificadores existentes.

MERGE INTO CONVERSATIONS_STATUSES target
USING (
    SELECT '81000000-0000-0000-0000-000000000001' AS ID
    FROM DUAL
) source
ON (target.CONVERSATIONS_STATUSES_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (CONVERSATIONS_STATUSES_ID, NAME_STATUS, CREATED_AT)
    VALUES (source.ID, 'Abierta', SYSTIMESTAMP);

MERGE INTO SENDER_TYPES target
USING (
    SELECT '82000000-0000-0000-0000-000000000001' AS ID
    FROM DUAL
) source
ON (target.SENDER_TYPES_ID = source.ID)
WHEN NOT MATCHED THEN
    INSERT (SENDER_TYPES_ID, NAME_TYPE, CREATED_AT)
    VALUES (source.ID, 'Cliente', SYSTIMESTAMP);

COMMIT;
