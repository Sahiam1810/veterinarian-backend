-- =============================================================================
-- DATOS DE DEMO PARA PRUEBAS (servicios, medicamentos, procedimientos, insumos,
-- permisos de los módulos nuevos, disponibilidad, dueños y mascotas)
-- Base de datos: Oracle Database
--
-- Requisitos antes de correrlo:
--   1. Migraciones aplicadas (dotnet ef database update).
--   2. Ya corriste insert_all_seeds.sql (o extra/apply_all.sql): necesita los
--      roles, los veterinarios y las especies/razas.
--
-- Es IDEMPOTENTE: se puede correr varias veces. Cada fila se busca por nombre
-- (o cédula) y solo se inserta si no existe; nunca pisa lo que ya editaste.
-- Los IDs se generan solos, así que no choca con los GUID fijos de los otros seeds.
--
-- Uso en SQL Developer:  SET DEFINE OFF;  @ruta\demo_data_pruebas.sql
-- =============================================================================
SET DEFINE OFF;
SET SERVEROUTPUT ON;

-- =============================================================================
-- 1. MÓDULOS NUEVOS (mismos ids/nombres que extra/modules_seed.sql)
-- =============================================================================
MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000025' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Órdenes Médicas'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Órdenes Médicas', 'Órdenes de medicamentos y procedimientos médicos', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000026' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Hospitalización'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Hospitalización', 'Admisión, notas de turno y alta de estancias hospitalarias', SYSTIMESTAMP);

MERGE INTO MODULES target
USING (SELECT 'a1000000-0000-0000-0000-000000000027' AS ID FROM DUAL) source
ON (target.MODULE_ID = source.ID OR UPPER(target.NAME) = UPPER('Insumos'))
WHEN NOT MATCHED THEN
    INSERT (MODULE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ID, 'Insumos', 'Catálogo de insumos y registro de su consumo en hospitalización', SYSTIMESTAMP);

-- =============================================================================
-- 2. PERMISOS POR ROL PARA LOS MÓDULOS NUEVOS
-- Solo inserta si el rol aún no tiene fila para ese módulo (no pisa ajustes
-- hechos desde SuperAdmin). Después de correrlo, cerrar sesión y volver a entrar:
-- los permisos viajan en el token.
--                          Ver Crear Editar Eliminar
--   Administrador           1    1     1      1     (los tres módulos)
--   Veterinario             1    1     1      0     Órdenes Médicas y Hospitalización
--                           1    1     0      0     Insumos (registra consumos)
--   Recepcionista           1    0     1      0     Órdenes Médicas (cola de pendientes)
--   Auxiliar                1    0     1      0     Órdenes Médicas
--                           1    1     0      0     Hospitalización e Insumos
-- =============================================================================
DECLARE
    PROCEDURE grant_perm(
        p_role_name VARCHAR2,
        p_module    VARCHAR2,
        p_view      NUMBER,
        p_create    NUMBER,
        p_edit      NUMBER,
        p_delete    NUMBER) IS
        v_module_id VARCHAR2(36);
        v_role_id   VARCHAR2(36);
    BEGIN
        SELECT MIN(ROLE_ID) INTO v_role_id FROM ROLES WHERE UPPER(NAME) = UPPER(p_role_name);
        IF v_role_id IS NULL THEN
            DBMS_OUTPUT.PUT_LINE('AVISO: no existe el rol ' || p_role_name || ' (se omite ' || p_module || ')');
            RETURN;
        END IF;
        SELECT MODULE_ID INTO v_module_id FROM MODULES WHERE UPPER(NAME) = UPPER(p_module);

        MERGE INTO ROLE_PERMISSIONS target
        USING (SELECT v_role_id ROLE_ID, v_module_id MODULE_ID FROM DUAL) source
        ON (target.ROLE_ID = source.ROLE_ID AND target.MODULE_ID = source.MODULE_ID)
        WHEN NOT MATCHED THEN
            INSERT (ROLE_PERMISSION_ID, ROLE_ID, MODULE_ID,
                    CAN_VIEW, CAN_CREATE, CAN_EDIT, CAN_DELETE, CREATED_AT)
            VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    source.ROLE_ID, source.MODULE_ID,
                    p_view, p_create, p_edit, p_delete, SYSTIMESTAMP);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('AVISO: no existe el módulo ' || p_module);
    END;
BEGIN
    -- Administrador
    grant_perm('Administrador', 'Órdenes Médicas', 1, 1, 1, 1);
    grant_perm('Administrador', 'Hospitalización', 1, 1, 1, 1);
    grant_perm('Administrador', 'Insumos',         1, 1, 1, 1);
    -- Veterinario
    grant_perm('Veterinario', 'Órdenes Médicas', 1, 1, 1, 0);
    grant_perm('Veterinario', 'Hospitalización', 1, 1, 1, 0);
    grant_perm('Veterinario', 'Insumos',         1, 1, 0, 0);
    -- Recepcionista
    grant_perm('Recepcionista', 'Órdenes Médicas', 1, 0, 1, 0);
    -- Auxiliar
    grant_perm('Auxiliar', 'Órdenes Médicas', 1, 0, 1, 0);
    grant_perm('Auxiliar', 'Hospitalización', 1, 1, 0, 0);
    grant_perm('Auxiliar', 'Insumos',         1, 1, 0, 0);
END;
/

-- =============================================================================
-- 3. TIPOS DE SERVICIO Y SERVICIOS (con precio en COP y duración)
-- =============================================================================
DECLARE
    PROCEDURE add_type(p_name VARCHAR2, p_desc VARCHAR2) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM TYPE_SERVICES WHERE UPPER(NAME) = UPPER(p_name);
        IF v_count = 0 THEN
            INSERT INTO TYPE_SERVICES (TYPE_SERVICE_ID, NAME, DESCRIPTION, CREATED_AT)
            VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    p_name, p_desc, SYSTIMESTAMP);
        END IF;
    END;

    PROCEDURE add_service(p_type VARCHAR2, p_name VARCHAR2, p_minutes NUMBER, p_price NUMBER) IS
        v_count   NUMBER;
        v_type_id VARCHAR2(36);
    BEGIN
        SELECT COUNT(*) INTO v_count FROM SERVICES WHERE UPPER(NAME) = UPPER(p_name);
        IF v_count > 0 THEN
            RETURN;
        END IF;
        SELECT MIN(TYPE_SERVICE_ID) INTO v_type_id FROM TYPE_SERVICES WHERE UPPER(NAME) = UPPER(p_type);
        INSERT INTO SERVICES (SERVICE_ID, TYPE_SERVICE_ID, NAME, DURATION_MINUTES, PRICE, IS_ACTIVE, CREATED_AT)
        VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                v_type_id, p_name, p_minutes, p_price, 'Y', SYSTIMESTAMP);
    END;
BEGIN
    add_type('Consulta Médica',      'Consultas de valoración general y especializada');
    add_type('Medicina Preventiva',  'Vacunación, desparasitación y planes de prevención');
    add_type('Cirugía',              'Intervenciones quirúrgicas y procedimientos con anestesia');
    add_type('Diagnóstico',          'Laboratorio e imágenes diagnósticas');
    add_type('Urgencia',             'Atención veterinaria prioritaria');
    add_type('Estética y Bienestar', 'Baño, corte y cuidado general');
    add_type('Odontología',          'Salud dental');

    -- Consulta Médica
    add_service('Consulta Médica', 'Consulta General',                    30,  50000);
    add_service('Consulta Médica', 'Consulta de Control',                 20,  35000);
    add_service('Consulta Médica', 'Consulta Especializada',              45,  90000);
    add_service('Consulta Médica', 'Consulta Dermatología',               40,  85000);
    add_service('Consulta Médica', 'Consulta Cardiología',                45, 120000);
    add_service('Consulta Médica', 'Consulta Oftalmología',               40,  95000);
    add_service('Consulta Médica', 'Consulta Nutrición',                  40,  70000);
    add_service('Consulta Médica', 'Segunda Opinión Clínica',             45,  80000);
    add_service('Consulta Médica', 'Certificado de Salud para Viaje',     30,  60000);

    -- Medicina Preventiva
    add_service('Medicina Preventiva', 'Aplicación de Vacuna',            20,  40000);
    add_service('Medicina Preventiva', 'Vacuna Antirrábica',              15,  45000);
    add_service('Medicina Preventiva', 'Vacuna Múltiple Canina',          20,  65000);
    add_service('Medicina Preventiva', 'Vacuna Triple Felina',            20,  60000);
    add_service('Medicina Preventiva', 'Vacuna Leucemia Felina',          20,  70000);
    add_service('Medicina Preventiva', 'Vacuna Tos de las Perreras',      15,  55000);
    add_service('Medicina Preventiva', 'Desparasitación Interna',         15,  25000);
    add_service('Medicina Preventiva', 'Desparasitación Externa',         15,  30000);
    add_service('Medicina Preventiva', 'Plan Cachorro (Paquete Completo)',45, 180000);
    add_service('Medicina Preventiva', 'Colocación de Microchip',         20,  75000);

    -- Cirugía
    add_service('Cirugía', 'Esterilización Canina Hembra',               90, 380000);
    add_service('Cirugía', 'Castración Canina Macho',                    60, 280000);
    add_service('Cirugía', 'Esterilización Felina Hembra',               60, 250000);
    add_service('Cirugía', 'Castración Felina Macho',                    40, 150000);
    add_service('Cirugía', 'Sutura de Heridas',                          40, 120000);
    add_service('Cirugía', 'Drenaje de Absceso',                         40,  95000);
    add_service('Cirugía', 'Cirugía General (Valoración)',               60, 450000);

    -- Diagnóstico
    add_service('Diagnóstico', 'Hemograma Completo',                     20,  55000);
    add_service('Diagnóstico', 'Química Sanguínea',                      20,  95000);
    add_service('Diagnóstico', 'Uroanálisis',                            20,  45000);
    add_service('Diagnóstico', 'Coproscópico',                           20,  35000);
    add_service('Diagnóstico', 'Perfil Prequirúrgico',                   30, 150000);
    add_service('Diagnóstico', 'Radiografía Simple',                     30,  90000);
    add_service('Diagnóstico', 'Ecografía Abdominal',                    40, 130000);
    add_service('Diagnóstico', 'Electrocardiograma',                     30,  80000);
    add_service('Diagnóstico', 'Test Rápido Parvovirus',                 15,  70000);

    -- Urgencia
    add_service('Urgencia', 'Urgencia Diurna',                           45, 120000);
    add_service('Urgencia', 'Urgencia Nocturna',                         45, 180000);
    add_service('Urgencia', 'Fluidoterapia Ambulatoria',                 60, 110000);

    -- Estética y Bienestar
    add_service('Estética y Bienestar', 'Baño y Corte Higiénico',        60,  60000);
    add_service('Estética y Bienestar', 'Baño Medicado',                 60,  80000);
    add_service('Estética y Bienestar', 'Corte de Uñas',                 15,  20000);
    add_service('Estética y Bienestar', 'Limpieza de Oídos',             20,  25000);
    add_service('Estética y Bienestar', 'Limpieza de Glándulas Anales',  15,  25000);

    -- Odontología
    add_service('Odontología', 'Profilaxis Dental',                      60, 220000);
    add_service('Odontología', 'Extracción Dental Simple',               45, 130000);
END;
/

-- =============================================================================
-- 4. CATÁLOGOS DE ÓRDENES MÉDICAS: MEDICAMENTOS Y PROCEDIMIENTOS
-- Son los que salen (con buscador) al anexar una orden en la consulta.
-- =============================================================================
DECLARE
    PROCEDURE add_med(p_name VARCHAR2, p_code VARCHAR2) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM MEDICATIONS WHERE UPPER(NAME) = UPPER(p_name);
        IF v_count = 0 THEN
            INSERT INTO MEDICATIONS (MEDICATION_ID, NAME, CODE, IS_ACTIVE, CREATED_AT)
            VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    p_name, p_code, 1, SYSTIMESTAMP);
        END IF;
    END;
BEGIN
    -- Antibióticos
    add_med('Amoxicilina 500 mg tabletas',                       'MED-001');
    add_med('Amoxicilina + Ácido clavulánico 500/125 mg',        'MED-002');
    add_med('Cefalexina 500 mg cápsulas',                        'MED-003');
    add_med('Enrofloxacina 50 mg tabletas',                      'MED-004');
    add_med('Doxiciclina 100 mg tabletas',                       'MED-005');
    add_med('Metronidazol 250 mg tabletas',                      'MED-006');
    add_med('Clindamicina 150 mg cápsulas',                      'MED-007');
    add_med('Azitromicina 250 mg tabletas',                      'MED-008');
    add_med('Trimetoprim + Sulfametoxazol 480 mg',               'MED-009');
    -- Antiinflamatorios y analgésicos
    add_med('Meloxicam 1.5 mg/ml suspensión oral',               'MED-010');
    add_med('Meloxicam 5 mg/ml inyectable',                      'MED-011');
    add_med('Carprofeno 75 mg tabletas',                         'MED-012');
    add_med('Tramadol 50 mg tabletas',                           'MED-013');
    add_med('Gabapentina 100 mg cápsulas',                       'MED-014');
    add_med('Prednisolona 5 mg tabletas',                        'MED-015');
    add_med('Dexametasona 4 mg/ml inyectable',                   'MED-016');
    add_med('Buprenorfina 0.3 mg/ml inyectable',                 'MED-017');
    -- Gastrointestinales
    add_med('Omeprazol 20 mg cápsulas',                          'MED-018');
    add_med('Sucralfato 1 g tabletas',                           'MED-019');
    add_med('Metoclopramida 10 mg tabletas',                     'MED-020');
    add_med('Maropitant 16 mg tabletas (antiemético)',           'MED-021');
    add_med('Lactulosa jarabe',                                  'MED-022');
    add_med('Probiótico canino y felino en pasta',               'MED-023');
    -- Antiparasitarios
    add_med('Ivermectina 1% inyectable',                         'MED-024');
    add_med('Praziquantel + Pirantel tabletas',                  'MED-025');
    add_med('Fenbendazol 500 mg tabletas',                       'MED-026');
    add_med('Milbemicina + Praziquantel tabletas',               'MED-027');
    add_med('Afoxolaner masticable (pulgas y garrapatas)',       'MED-028');
    add_med('Fluralaner masticable (pulgas y garrapatas)',       'MED-029');
    add_med('Selamectina pipeta tópica',                         'MED-030');
    -- Cardiología
    add_med('Furosemida 40 mg tabletas',                         'MED-031');
    add_med('Enalapril 5 mg tabletas',                           'MED-032');
    add_med('Pimobendan 5 mg cápsulas',                          'MED-033');
    -- Dermatología y alergias
    add_med('Oclacitinib 16 mg tabletas',                        'MED-034');
    add_med('Cetirizina 10 mg tabletas',                         'MED-035');
    add_med('Difenhidramina 25 mg tabletas',                     'MED-036');
    add_med('Ketoconazol 200 mg tabletas',                       'MED-037');
    add_med('Itraconazol 100 mg cápsulas',                       'MED-038');
    add_med('Champú de clorhexidina 4%',                         'MED-039');
    -- Oído y ojo
    add_med('Gotas óticas antibiótico + antimicótico',           'MED-040');
    add_med('Limpiador ótico',                                   'MED-041');
    add_med('Colirio de gentamicina',                            'MED-042');
    add_med('Ciprofloxacina colirio',                            'MED-043');
    add_med('Lágrimas artificiales gel',                         'MED-044');
    -- Neurología y sedación
    add_med('Fenobarbital 100 mg tabletas',                      'MED-045');
    add_med('Diazepam 5 mg/ml inyectable',                       'MED-046');
    -- Fluidos, vitaminas y suplementos
    add_med('Suero fisiológico 0.9% (fluidoterapia)',            'MED-047');
    add_med('Ringer lactato (fluidoterapia)',                    'MED-048');
    add_med('Complejo B inyectable',                             'MED-049');
    add_med('Hierro dextrano inyectable',                        'MED-050');
    add_med('Omega 3 cápsulas',                                  'MED-051');
    add_med('Condroprotector (glucosamina + condroitina)',       'MED-052');
    add_med('Suplemento vitamínico en pasta',                    'MED-053');
END;
/

DECLARE
    PROCEDURE add_proc(p_name VARCHAR2, p_code VARCHAR2) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM PROCEDURES WHERE UPPER(NAME) = UPPER(p_name);
        IF v_count = 0 THEN
            INSERT INTO PROCEDURES (PROCEDURE_ID, NAME, CODE, IS_ACTIVE, CREATED_AT)
            VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    p_name, p_code, 1, SYSTIMESTAMP);
        END IF;
    END;
BEGIN
    -- Radiografías
    add_proc('Radiografía de tórax (2 proyecciones)',            'RX-001');
    add_proc('Radiografía de abdomen',                           'RX-002');
    add_proc('Radiografía de columna',                           'RX-003');
    add_proc('Radiografía de extremidad anterior',               'RX-004');
    add_proc('Radiografía de extremidad posterior',              'RX-005');
    add_proc('Radiografía de cadera (displasia)',                'RX-006');
    add_proc('Radiografía de cráneo y dental',                   'RX-007');
    add_proc('Radiografía con contraste (gastrointestinal)',     'RX-008');
    -- Ecografías y cardiología
    add_proc('Ecografía abdominal',                              'ECO-001');
    add_proc('Ecografía gestacional',                            'ECO-002');
    add_proc('Ecocardiograma',                                   'ECO-003');
    add_proc('Electrocardiograma',                               'CAR-001');
    add_proc('Toma de presión arterial',                         'CAR-002');
    -- Laboratorio clínico
    add_proc('Hemograma completo',                               'LAB-001');
    add_proc('Química sanguínea (perfil renal y hepático)',      'LAB-002');
    add_proc('Glicemia',                                         'LAB-003');
    add_proc('Uroanálisis completo',                             'LAB-004');
    add_proc('Urocultivo y antibiograma',                        'LAB-005');
    add_proc('Coproscópico (huevos de parásitos)',               'LAB-006');
    add_proc('Frotis sanguíneo (hemoparásitos)',                 'LAB-007');
    add_proc('Citología de piel u oído',                         'LAB-008');
    add_proc('Raspado cutáneo',                                  'LAB-009');
    add_proc('Cultivo micológico (hongos)',                      'LAB-010');
    add_proc('Test rápido parvovirus canino',                    'LAB-011');
    add_proc('Test rápido moquillo canino',                      'LAB-012');
    add_proc('Test rápido FIV / FeLV (felinos)',                 'LAB-013');
    add_proc('Test rápido Ehrlichia / Anaplasma',                'LAB-014');
    add_proc('Perfil tiroideo (T4)',                             'LAB-015');
    add_proc('Tiempos de coagulación',                           'LAB-016');
    add_proc('Perfil prequirúrgico',                             'LAB-017');
    add_proc('Biopsia y estudio histopatológico',                'LAB-018');
    -- Procedimientos clínicos y quirúrgicos
    add_proc('Esterilización / castración',                      'CIR-001');
    add_proc('Sutura de heridas',                                'CIR-002');
    add_proc('Drenaje de absceso',                               'CIR-003');
    add_proc('Extracción de cuerpo extraño',                     'CIR-004');
    add_proc('Cistotomía',                                       'CIR-005');
    add_proc('Mastectomía',                                      'CIR-006');
    add_proc('Cesárea',                                          'CIR-007');
    add_proc('Profilaxis dental (limpieza con ultrasonido)',     'ODO-001');
    add_proc('Extracción dental',                                'ODO-002');
    add_proc('Colocación de catéter intravenoso',                'CLI-001');
    add_proc('Fluidoterapia intravenosa',                        'CLI-002');
    add_proc('Curación y vendaje',                               'CLI-003');
    add_proc('Limpieza y lavado de oídos',                       'CLI-004');
    add_proc('Test de Schirmer (lagrimeo)',                      'OFT-001');
    add_proc('Tinción con fluoresceína (úlcera corneal)',        'OFT-002');
    add_proc('Tonometría (presión intraocular)',                 'OFT-003');
    add_proc('Endoscopia',                                       'ESP-001');
    add_proc('Tomografía computarizada (remisión)',              'ESP-002');
    add_proc('Resonancia magnética (remisión)',                  'ESP-003');
END;
/

-- =============================================================================
-- 5. INSUMOS (catálogo con precio y stock) para el consumo en hospitalización
-- =============================================================================
DECLARE
    PROCEDURE add_supply(p_name VARCHAR2, p_unit VARCHAR2, p_price NUMBER, p_stock NUMBER) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM SUPPLIES WHERE UPPER(NAME) = UPPER(p_name);
        IF v_count = 0 THEN
            INSERT INTO SUPPLIES (SUPPLY_ID, NAME, UNIT, UNIT_PRICE, STOCK, IS_ACTIVE, CREATED_AT)
            VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    p_name, p_unit, p_price, p_stock, 1, SYSTIMESTAMP);
        END IF;
    END;
BEGIN
    add_supply('Jeringa desechable 3 ml',                        'unidad',   700,  400);
    add_supply('Jeringa desechable 5 ml',                        'unidad',   800,  400);
    add_supply('Jeringa desechable 10 ml',                       'unidad',  1100,  300);
    add_supply('Jeringa de insulina 1 ml',                       'unidad',   900,  200);
    add_supply('Aguja hipodérmica 21G',                          'unidad',   300,  500);
    add_supply('Catéter intravenoso 22G',                        'unidad',  4500,  120);
    add_supply('Catéter intravenoso 24G',                        'unidad',  4500,  120);
    add_supply('Equipo de venoclisis (macrogotero)',             'unidad',  3500,  100);
    add_supply('Solución salina 0.9% 500 ml',                    'unidad',  6500,   80);
    add_supply('Ringer lactato 500 ml',                          'unidad',  7000,   80);
    add_supply('Ringer lactato 1000 ml',                         'unidad', 11000,   60);
    add_supply('Gasas estériles 10x10 (paquete x10)',            'paquete', 4000,  150);
    add_supply('Algodón hidrófilo',                              'paquete', 5000,   60);
    add_supply('Esparadrapo hipoalergénico 2.5 cm',              'unidad',  3200,   80);
    add_supply('Venda elástica autoadherente',                   'unidad',  4800,   90);
    add_supply('Guantes de látex (par)',                         'par',      900,  400);
    add_supply('Guantes estériles (par)',                        'par',     2800,  150);
    add_supply('Alcohol antiséptico 70%',                        'ml',        12, 5000);
    add_supply('Clorhexidina solución 2%',                       'ml',        35, 3000);
    add_supply('Yodopovidona solución',                          'ml',        28, 3000);
    add_supply('Sutura nylon 3-0',                               'unidad',  9500,   60);
    add_supply('Sutura absorbible 3-0',                          'unidad', 12500,   60);
    add_supply('Hoja de bisturí',                                'unidad',  1600,  150);
    add_supply('Collar isabelino talla M',                       'unidad', 18000,   25);
    add_supply('Tapete absorbente para jaula',                   'unidad',  2500,  200);
    add_supply('Sonda uretral felina',                           'unidad',  6500,   40);
    add_supply('Sonda nasogástrica',                             'unidad',  7500,   40);
    add_supply('Tubo de muestra EDTA (tapa lila)',               'unidad',  1200,  300);
    add_supply('Tubo de muestra sin anticoagulante (tapa roja)', 'unidad',  1200,  300);
    add_supply('Alimento hospitalario recuperación (lata)',      'lata',   16000,   50);
    add_supply('Agua estéril para inyección 10 ml',              'unidad',  1500,  100);
END;
/

-- =============================================================================
-- 6. DISPONIBILIDAD: horario Lunes-Viernes 07:00-17:00 (citas de 30 min) para
-- cada veterinario, en los días en que aún no tenga ningún bloque.
-- DAY_OF_WEEK sigue System.DayOfWeek de .NET: 1=Lunes ... 5=Viernes.
-- =============================================================================
DECLARE
    v_count NUMBER;
BEGIN
    FOR vet IN (SELECT VETERINARIAN_ID FROM VETERINARIANS) LOOP
        FOR d IN 1..5 LOOP
            SELECT COUNT(*) INTO v_count
              FROM AVAILABILITIES
             WHERE VETERINARIAN_ID = vet.VETERINARIAN_ID AND DAY_OF_WEEK = d;
            IF v_count = 0 THEN
                INSERT INTO AVAILABILITIES (
                    AVAILABILITY_ID, VETERINARIAN_ID, DAY_OF_WEEK, START_TIME, END_TIME,
                    IS_ACTIVE, SLOT_DURATION_MINUTES, MAX_CONCURRENT_APPOINTMENTS, CREATED_AT)
                VALUES (
                    LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    vet.VETERINARIAN_ID, d, '07:00:00', '17:00:00',
                    'Y', 30, 1, SYSTIMESTAMP);
            END IF;
        END LOOP;
    END LOOP;
END;
/

-- =============================================================================
-- 7. DUEÑOS Y MASCOTAS DE DEMO (para que Dueños y Mascotas se vean llenos)
-- Dueños por cédula, mascotas por (dueño + nombre). Especie y raza se buscan por
-- nombre y funcionan con cualquiera de los dos catálogos (Perro/Gato o Canino/Felino);
-- si la raza pedida no existe se usa cualquier raza de esa especie.
-- =============================================================================
DECLARE
    PROCEDURE add_client(p_name VARCHAR2, p_email VARCHAR2, p_ident VARCHAR2,
                         p_phone VARCHAR2, p_address VARCHAR2) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM CLIENTS
         WHERE IDENTIFICATION_NUMBER = p_ident OR UPPER(EMAIL) = UPPER(p_email);
        IF v_count = 0 THEN
            INSERT INTO CLIENTS (CLIENT_ID, FULL_NAME, EMAIL, IDENTIFICATION_NUMBER,
                                 ADDRESS, PHONE_NUMBER, IS_ACTIVE, CREATED_AT)
            VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    p_name, p_email, p_ident, p_address, p_phone, 1, SYSTIMESTAMP);
        END IF;
    END;

    PROCEDURE add_pet(p_ident VARCHAR2, p_name VARCHAR2, p_age NUMBER, p_gender VARCHAR2,
                      p_weight NUMBER, p_species_a VARCHAR2, p_species_b VARCHAR2,
                      p_race_kw VARCHAR2, p_obs VARCHAR2) IS
        v_client_id  VARCHAR2(36);
        v_species_id VARCHAR2(36);
        v_race_id    VARCHAR2(36);
        v_pet_id     VARCHAR2(36);
        v_count      NUMBER;
    BEGIN
        SELECT MIN(CLIENT_ID) INTO v_client_id FROM CLIENTS WHERE IDENTIFICATION_NUMBER = p_ident;
        IF v_client_id IS NULL THEN
            DBMS_OUTPUT.PUT_LINE('AVISO: no existe el dueño ' || p_ident || ' (se omite ' || p_name || ')');
            RETURN;
        END IF;

        SELECT COUNT(*) INTO v_count
          FROM CLIENTS_PETS cp JOIN PETS p ON p.PET_ID = cp.PET_ID
         WHERE cp.CLIENT_ID = v_client_id AND UPPER(p.NAME) = UPPER(p_name);
        IF v_count > 0 THEN
            RETURN;
        END IF;

        SELECT MIN(SPECIES_ID) INTO v_species_id FROM SPECIES
         WHERE UPPER(NAME) IN (UPPER(p_species_a), UPPER(p_species_b));
        IF v_species_id IS NULL THEN
            DBMS_OUTPUT.PUT_LINE('AVISO: no existe la especie ' || p_species_a || ' (se omite ' || p_name || ')');
            RETURN;
        END IF;

        SELECT MIN(RACE_ID) INTO v_race_id FROM RACES
         WHERE SPECIES_ID = v_species_id AND UPPER(NAME) LIKE '%' || UPPER(p_race_kw) || '%';
        IF v_race_id IS NULL THEN
            SELECT MIN(RACE_ID) INTO v_race_id FROM RACES WHERE SPECIES_ID = v_species_id;
        END IF;
        IF v_race_id IS NULL THEN
            DBMS_OUTPUT.PUT_LINE('AVISO: la especie ' || p_species_a || ' no tiene razas (se omite ' || p_name || ')');
            RETURN;
        END IF;

        v_pet_id := LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5'));
        INSERT INTO PETS (PET_ID, NAME, AGE, GENDER, WEIGHT, OBSERVATIONS, SPECIES_ID, RACE_ID, CREATED_AT)
        VALUES (v_pet_id, p_name, p_age, p_gender, p_weight, p_obs, v_species_id, v_race_id, SYSTIMESTAMP);
        INSERT INTO CLIENTS_PETS (CLIENT_PET_ID, CLIENT_ID, PET_ID, IS_PRIMARY_OWNER, CREATED_AT)
        VALUES (LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                v_client_id, v_pet_id, 'Y', SYSTIMESTAMP);
    END;
BEGIN
    add_client('Camila Andrea Rojas',     'camila.rojas@correo.test',     '1020304051', '3115550101', 'Carrera 15 #85-20, Bogotá');
    add_client('Juan Sebastián Pardo',    'juan.pardo@correo.test',       '1020304052', '3125550102', 'Calle 100 #19-54, Bogotá');
    add_client('María Fernanda Castillo', 'maria.castillo@correo.test',   '1020304053', '3135550103', 'Avenida 68 #45-10, Bogotá');
    add_client('Carlos Eduardo Molina',   'carlos.molina@correo.test',    '1020304054', '3145550104', 'Carrera 7 #72-41, Bogotá');
    add_client('Laura Valentina Gómez',   'laura.gomez@correo.test',      '1020304055', '3155550105', 'Calle 53 #13-27, Bogotá');
    add_client('Andrés Felipe Herrera',   'andres.herrera@correo.test',   '1020304056', '3165550106', 'Carrera 30 #12-08, Bogotá');
    add_client('Paola Andrea Suárez',     'paola.suarez@correo.test',     '1020304057', '3175550107', 'Calle 134 #7-83, Bogotá');
    add_client('Diego Alejandro Ramírez', 'diego.ramirez@correo.test',    '1020304058', '3185550108', 'Transversal 23 #94-33, Bogotá');
    add_client('Natalia Isabel Vargas',   'natalia.vargas@correo.test',   '1020304059', '3195550109', 'Carrera 11 #93-15, Bogotá');
    add_client('Santiago José Peña',      'santiago.pena@correo.test',    '1020304060', '3205550110', 'Calle 80 #69-70, Bogotá');
    add_client('Daniela Carolina Ortiz',  'daniela.ortiz@correo.test',    '1020304061', '3215550111', 'Carrera 50 #127-30, Bogotá');
    add_client('Felipe Augusto Lozano',   'felipe.lozano@correo.test',    '1020304062', '3225550112', 'Calle 26 #59-51, Bogotá');

    add_pet('1020304051', 'Simba',   4, 'M', 24.500, 'Perro', 'Canino', 'Labrador',  'Muy activo, come con avidez');
    add_pet('1020304051', 'Mía',     2, 'F',  3.800, 'Gato',  'Felino', 'Siam',      'Vacunas al día');
    add_pet('1020304052', 'Thor',    6, 'M', 32.000, 'Perro', 'Canino', 'Pastor',    'Displasia de cadera leve');
    add_pet('1020304053', 'Coco',    3, 'F',  6.200, 'Perro', 'Canino', 'Poodle',    'Alergia a picaduras de pulga');
    add_pet('1020304053', 'Nube',    5, 'F',  4.400, 'Gato',  'Felino', 'Persa',     'Pelaje largo, requiere cepillado');
    add_pet('1020304054', 'Max',     8, 'M', 29.000, 'Perro', 'Canino', 'Golden',    'Artrosis en manejo');
    add_pet('1020304055', 'Kira',    1, 'F',  9.500, 'Perro', 'Canino', 'Beagle',    'Cachorra, plan de vacunación');
    add_pet('1020304055', 'Tigre',   2, 'M',  4.900, 'Gato',  'Felino', 'Mestizo',   'Rescatado');
    add_pet('1020304056', 'Bruno',   5, 'M', 11.800, 'Perro', 'Canino', 'Bulldog',   'Problemas respiratorios con el calor');
    add_pet('1020304057', 'Luna',    3, 'F',  5.100, 'Gato',  'Felino', 'Mestizo',   'Esterilizada');
    add_pet('1020304058', 'Rocco',   7, 'M', 38.000, 'Perro', 'Canino', 'Pastor',    'Vigilar peso');
    add_pet('1020304058', 'Pepa',    4, 'F',  4.200, 'Gato',  'Felino', 'Angora',    'Dieta renal');
    add_pet('1020304059', 'Lola',    2, 'F',  7.300, 'Perro', 'Canino', 'Cocker',    'Otitis recurrente');
    add_pet('1020304060', 'Zeus',    9, 'M', 27.000, 'Perro', 'Canino', 'Labrador',  'Cardiópata en control');
    add_pet('1020304061', 'Canela',  1, 'F',  2.300, 'Conejo','Conejo', 'Holand',    'Revisión dental');
    add_pet('1020304061', 'Kiwi',    2, 'M',  0.090, 'Ave',   'Ave',    'Periquito', 'Plumaje en muda');
    add_pet('1020304062', 'Milo',    3, 'M',  5.600, 'Gato',  'Felino', 'Bengal',    'Muy juguetón');
    add_pet('1020304062', 'Rex',     10,'M', 22.000, 'Perro', 'Canino', 'Mestizo',   'Geriátrico, chequeo semestral');
END;
/

COMMIT;

-- =============================================================================
-- 8. RESUMEN (conteos después de correr el script)
-- =============================================================================
SELECT 'Servicios activos' AS CATALOGO, COUNT(*) AS TOTAL FROM SERVICES WHERE IS_ACTIVE = 'Y'
UNION ALL SELECT 'Medicamentos', COUNT(*) FROM MEDICATIONS
UNION ALL SELECT 'Procedimientos', COUNT(*) FROM PROCEDURES
UNION ALL SELECT 'Insumos', COUNT(*) FROM SUPPLIES
UNION ALL SELECT 'Dueños (clientes)', COUNT(*) FROM CLIENTS
UNION ALL SELECT 'Mascotas', COUNT(*) FROM PETS
UNION ALL SELECT 'Bloques de disponibilidad', COUNT(*) FROM AVAILABILITIES;
