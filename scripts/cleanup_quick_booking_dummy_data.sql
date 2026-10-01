-- Script de limpieza de datos de prueba / relleno de Agendamiento Rapido.
-- Revisar el SELECT antes de ejecutar. NO correr automaticamente.

-- Las mascotas solo se consideran candidatas cuando su propietario conserva
-- los marcadores artificiales del flujo anterior. Esto evita limpiar mascotas
-- legitimas que tengan edad 0 o peso 0.01.
SELECT p.PET_ID,
       p.NAME,
       p.AGE,
       p.WEIGHT,
       c.CLIENT_ID,
       c.EMAIL,
       c.IDENTIFICATION_NUMBER
FROM PETS p
JOIN CLIENTS_PETS cp ON cp.PET_ID = p.PET_ID
JOIN CLIENTS c ON c.CLIENT_ID = cp.CLIENT_ID
WHERE (LOWER(c.EMAIL) LIKE 'pendiente-%@huellitas.local'
       OR LOWER(c.EMAIL) LIKE 'pending-%@huellitas.local'
       OR c.IDENTIFICATION_NUMBER LIKE 'PEND-%')
  AND (p.AGE = 0 OR p.WEIGHT = 0.01);

UPDATE PETS p
SET p.AGE = NULL,
    p.WEIGHT = NULL
WHERE (p.AGE = 0 OR p.WEIGHT = 0.01)
  AND EXISTS (
      SELECT 1
      FROM CLIENTS_PETS cp
      JOIN CLIENTS c ON c.CLIENT_ID = cp.CLIENT_ID
      WHERE cp.PET_ID = p.PET_ID
        AND (LOWER(c.EMAIL) LIKE 'pendiente-%@huellitas.local'
             OR LOWER(c.EMAIL) LIKE 'pending-%@huellitas.local'
             OR c.IDENTIFICATION_NUMBER LIKE 'PEND-%')
  );

UPDATE CLIENTS
SET EMAIL = NULL
WHERE LOWER(EMAIL) LIKE 'pendiente-%@huellitas.local'
   OR LOWER(EMAIL) LIKE 'pending-%@huellitas.local';

UPDATE CLIENTS
SET IDENTIFICATION_NUMBER = NULL
WHERE IDENTIFICATION_NUMBER LIKE 'PEND-%';

COMMIT;
