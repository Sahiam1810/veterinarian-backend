-- Catálogos veterinarios mínimos para una instalación nueva.
-- No crea servicios con precio, razas ni diagnósticos clínicos.
SET DEFINE OFF;

MERGE INTO TYPE_SERVICES target
USING (
    SELECT '87000000-0000-0000-0000-000000000001' ID, 'Consulta' NAME,
           'Valoración veterinaria general o especializada' DESCRIPTION FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000002', 'Vacunación',
           'Aplicación y seguimiento de vacunas' FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000003', 'Procedimiento',
           'Procedimiento ambulatorio o quirúrgico' FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000004', 'Diagnóstico',
           'Pruebas y ayudas diagnósticas' FROM DUAL UNION ALL
    SELECT '87000000-0000-0000-0000-000000000005', 'Urgencia',
           'Atención veterinaria prioritaria' FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN UPDATE SET target.DESCRIPTION = source.DESCRIPTION
WHEN NOT MATCHED THEN
    INSERT (TYPE_SERVICE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

MERGE INTO SPECIES target
USING (
    SELECT '88000000-0000-0000-0000-000000000001' ID, 'Perro' NAME FROM DUAL UNION ALL
    SELECT '88000000-0000-0000-0000-000000000002', 'Gato' FROM DUAL UNION ALL
    SELECT '88000000-0000-0000-0000-000000000003', 'Otro' FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN NOT MATCHED THEN
    INSERT (SPECIES_ID, NAME, CREATED_AT)
    VALUES (source.ID, source.NAME, SYSTIMESTAMP);

MERGE INTO SPECIALTIES target
USING (
    SELECT '89000000-0000-0000-0000-000000000001' ID, 'Medicina general' NAME,
           'Atención veterinaria general' DESCRIPTION FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000002', 'Cirugía',
           'Procedimientos quirúrgicos veterinarios' FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000003', 'Dermatología',
           'Diagnóstico y tratamiento dermatológico' FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000004', 'Medicina interna',
           'Diagnóstico y tratamiento de enfermedades internas' FROM DUAL UNION ALL
    SELECT '89000000-0000-0000-0000-000000000005', 'Urgencias',
           'Atención clínica veterinaria prioritaria' FROM DUAL
) source
ON (UPPER(target.NAME) = UPPER(source.NAME))
WHEN MATCHED THEN UPDATE SET target.DESCRIPTION = source.DESCRIPTION
WHEN NOT MATCHED THEN
    INSERT (SPECIALTY_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);

COMMIT;
