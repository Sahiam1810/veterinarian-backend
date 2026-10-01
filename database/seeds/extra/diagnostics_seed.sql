-- Catálogo inicial de diagnósticos clínicos veterinarios.
-- Ejecute este script después de aplicar las migraciones que crean DIAGNOSTICS.
-- Es idempotente por CODE (único): si el código ya existe, actualiza nombre/descripción;
-- solo inserta los códigos faltantes con los identificadores fijos del catálogo.

MERGE INTO DIAGNOSTICS target
USING (
    SELECT '8b000000-0000-0000-0000-000000000001' AS ID,
           'PREV' AS CODE,
           'Control preventivo / Vacunación de rutina' AS NAME,
           'Visita sin patología activa: control sano, vacunación o seguimiento preventivo' AS DESCRIPTION,
           1 AS IS_ACTIVE
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000002',
           'GASTRO',
           'Gastroenteritis aguda',
           'Inflamación gastrointestinal por dieta, infección o parasitosis',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000003',
           'DERM',
           'Dermatitis / alergia cutánea',
           'Alteraciones de piel: prurito, eritema, alopecia o lesiones alérgicas',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000004',
           'OTITIS',
           'Otitis',
           'Inflamación del oído externo o medio',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000005',
           'INFURI',
           'Infección urinaria',
           'Infección del tracto urinario inferior o cistitis',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000006',
           'RESP',
           'Infección respiratoria superior',
           'Tos, secreción nasal o signos de vías respiratorias altas',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000007',
           'PARAS',
           'Parasitosis intestinal',
           'Infestación por parásitos gastrointestinales',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000008',
           'TRAUMA',
           'Trauma / herida',
           'Lesión traumática, laceración o contusión',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000009',
           'ODONT',
           'Enfermedad periodontal',
           'Gingivitis, sarro o enfermedad dental',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000010',
           'OBES',
           'Sobrepeso / obesidad',
           'Exceso de peso con riesgo metabólico',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000011',
           'CONJ',
           'Conjuntivitis',
           'Inflamación ocular de la conjuntiva',
           1
    FROM DUAL
    UNION ALL
    SELECT '8b000000-0000-0000-0000-000000000012',
           'OTRO',
           'Otro / a determinar',
           'Diagnóstico provisional o no listado en el catálogo base',
           1
    FROM DUAL
) source
ON (UPPER(target.CODE) = UPPER(source.CODE))
WHEN MATCHED THEN
    UPDATE SET target.NAME = source.NAME,
               target.DESCRIPTION = source.DESCRIPTION,
               target.IS_ACTIVE = source.IS_ACTIVE,
               target.UPDATED_AT = SYSTIMESTAMP
WHEN NOT MATCHED THEN
    INSERT (ID, CODE, NAME, DESCRIPTION, IS_ACTIVE, CREATED_AT)
    VALUES (source.ID, source.CODE, source.NAME, source.DESCRIPTION, source.IS_ACTIVE, SYSTIMESTAMP);

COMMIT;
