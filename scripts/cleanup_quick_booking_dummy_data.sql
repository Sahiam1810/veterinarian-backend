-- Script de limpieza de datos de prueba / relleno de Agendamiento Rápido
-- NOTA IMPORTANTE: Entregar a Sahiam para revisar antes de ejecutar en producción o ambientes de prueba. NO CORRER AUTOMÁTICAMENTE.

-- 1. Limpieza de correos temporales/inventados en CLIENTS
UPDATE CLIENTS
SET EMAIL = NULL
WHERE LOWER(EMAIL) LIKE 'pendiente-%@huellitas.local'
   OR LOWER(EMAIL) LIKE 'pending-%@huellitas.local';

-- 2. Limpieza de números de identificación temporales/inventados en CLIENTS
UPDATE CLIENTS
SET IDENTIFICATION_NUMBER = NULL
WHERE IDENTIFICATION_NUMBER LIKE 'PEND-%';

-- 3. Limpieza de datos inventados de edad (0) y peso (0.01) en PETS creadas por agendamiento rápido
UPDATE PETS
SET AGE = NULL
WHERE AGE = 0;

UPDATE PETS
SET WEIGHT = NULL
WHERE WEIGHT = 0.01;

COMMIT;
