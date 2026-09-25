-- =============================================================================
-- SEED DE DATOS ABUNDANTES Y REALISTAS PARA PRUEBAS COMPLETAS DEL SISTEMA
-- Base de datos: Oracle Database
-- Es IDEMPOTENTE: no duplica registros existentes.
-- =============================================================================
SET DEFINE OFF;
SET SERVEROUTPUT ON;

-- 1. CLIENTES (DUEÑOS DE MASCOTAS)
DECLARE
    PROCEDURE add_client(p_id VARCHAR2, p_name VARCHAR2, p_email VARCHAR2, p_ident VARCHAR2,
                         p_phone VARCHAR2, p_address VARCHAR2) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM CLIENTS WHERE IDENTIFICATION_NUMBER = p_ident OR UPPER(EMAIL) = UPPER(p_email);
        IF v_count = 0 THEN
            INSERT INTO CLIENTS (CLIENT_ID, FULL_NAME, EMAIL, IDENTIFICATION_NUMBER, ADDRESS, PHONE_NUMBER, IS_ACTIVE, CREATED_AT)
            VALUES (p_id, p_name, p_email, p_ident, p_address, p_phone, 1, SYSTIMESTAMP - NUMTODSINTERVAL(TRUNC(DBMS_RANDOM.VALUE(1, 60)), 'DAY'));
        END IF;
    END;
BEGIN
    add_client('c1000000-0000-0000-0000-000000000001', 'Alejandro Morales',    'alejandro.morales@test.com', '1090111222', '3109990001', 'Calle 127 #15-40, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000002', 'Beatriz Quintana',    'beatriz.quintana@test.com', '1090111223', '3109990002', 'Carrera 9 #116-20, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000003', 'Camilo Cárdenas',     'camilo.cardenas@test.com',  '1090111224', '3109990003', 'Calle 170 #54-12, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000004', 'Diana Marcela Silva', 'diana.silva@test.com',     '1090111225', '3109990004', 'Carrera 7 #45-10, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000005', 'Esteban Gutiérrez',   'esteban.gutierrez@test.com','1090111226', '3109990005', 'Calle 85 #11-53, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000006', 'Flor Isabel Méndez',  'flor.mendez@test.com',      '1090111227', '3109990006', 'Carrera 15 #104-30, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000007', 'Gabriel Bermúdez',    'gabriel.bermudez@test.com', '1090111228', '3109990007', 'Calle 63 #24-18, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000008', 'Helena Restrepo',     'helena.restrepo@test.com',  '1090111229', '3109990008', 'Transversal 17 #98-05, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000009', 'Ignacio Villamizar', 'ignacio.villamizar@test.com','1090111230', '3109990009', 'Carrera 50 #80-12, Bogotá');
    add_client('c1000000-0000-0000-0000-000000000010', 'Juliana Pinzón',      'juliana.pinzon@test.com',   '1090111231', '3109990010', 'Calle 140 #19-45, Bogotá');
END;
/

-- 2. MASCOTAS Y CLIENTES_MASCOTAS
DECLARE
    PROCEDURE add_pet(p_id VARCHAR2, p_client_id VARCHAR2, p_name VARCHAR2, p_age NUMBER,
                      p_gender VARCHAR2, p_weight NUMBER, p_obs VARCHAR2,
                      p_species_name VARCHAR2) IS
        v_species_id VARCHAR2(36);
        v_race_id    VARCHAR2(36);
        v_cp_id      VARCHAR2(36);
        v_count      NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM PETS WHERE PET_ID = p_id OR (UPPER(NAME) = UPPER(p_name) AND AGE = p_age);
        IF v_count > 0 THEN RETURN; END IF;

        SELECT MIN(SPECIES_ID) INTO v_species_id FROM SPECIES WHERE UPPER(NAME) LIKE '%' || UPPER(p_species_name) || '%';
        IF v_species_id IS NULL THEN
            SELECT MIN(SPECIES_ID) INTO v_species_id FROM SPECIES;
        END IF;

        SELECT MIN(RACE_ID) INTO v_race_id FROM RACES WHERE SPECIES_ID = v_species_id;
        IF v_race_id IS NULL THEN
            SELECT MIN(RACE_ID) INTO v_race_id FROM RACES;
        END IF;

        INSERT INTO PETS (PET_ID, NAME, AGE, GENDER, WEIGHT, OBSERVATIONS, SPECIES_ID, RACE_ID, CREATED_AT)
        VALUES (p_id, p_name, p_age, p_gender, p_weight, p_obs, v_species_id, v_race_id, SYSTIMESTAMP - NUMTODSINTERVAL(TRUNC(DBMS_RANDOM.VALUE(1, 40)), 'DAY'));

        v_cp_id := LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5'));
        INSERT INTO CLIENTS_PETS (CLIENT_PET_ID, CLIENT_ID, PET_ID, IS_PRIMARY_OWNER, CREATED_AT)
        VALUES (v_cp_id, p_client_id, p_id, 'Y', SYSTIMESTAMP);
    END;
BEGIN
    add_pet('d1000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000001', 'Rocky',     5, 'M', 28.4, 'Vacunado y desparasitado', 'Canino');
    add_pet('d1000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000001', 'Michi',     2, 'F',  3.6, 'Gata esterilizada',       'Felino');
    add_pet('d1000000-0000-0000-0000-000000000003', 'c1000000-0000-0000-0000-000000000002', 'Boby',      4, 'M', 14.2, 'Alergia cutánea',        'Canino');
    add_pet('d1000000-0000-0000-0000-000000000004', 'c1000000-0000-0000-0000-000000000003', 'Sasha',     3, 'F', 22.0, 'Chequeo periódico',      'Canino');
    add_pet('d1000000-0000-0000-0000-000000000005', 'c1000000-0000-0000-0000-000000000004', 'Garfield',  6, 'M',  5.8, 'Sobrepeso controlado',    'Felino');
    add_pet('d1000000-0000-0000-0000-000000000006', 'c1000000-0000-0000-0000-000000000005', 'Mora',      1, 'F',  8.5, 'Cachorra activa',         'Canino');
    add_pet('d1000000-0000-0000-0000-000000000007', 'c1000000-0000-0000-0000-000000000006', 'Kiko',      2, 'M',  0.1, 'Ave doméstica',           'Ave');
    add_pet('d1000000-0000-0000-0000-000000000008', 'c1000000-0000-0000-0000-000000000007', 'Bugs',      2, 'M',  2.1, 'Dieta de fibra',          'Conejo');
    add_pet('d1000000-0000-0000-0000-000000000009', 'c1000000-0000-0000-0000-000000000008', 'Toby',      7, 'M', 31.0, 'Paciente geriátrico',     'Canino');
    add_pet('d1000000-0000-0000-0000-000000000010', 'c1000000-0000-0000-0000-000000000009', 'Nala',      4, 'F',  4.1, 'Vacunación al día',       'Felino');
END;
/

-- 3. CITAS EN DIVERSOS ESTADOS Y FECHAS
DECLARE
    v_status_agendada VARCHAR2(36) := 'aaaaaaaa-0000-0000-0000-000000000001';
    v_status_atendida VARCHAR2(36) := 'aaaaaaaa-0000-0000-0000-000000000002';
    v_status_cancelada VARCHAR2(36) := 'aaaaaaaa-0000-0000-0000-000000000003';
    v_status_noasistio VARCHAR2(36) := 'aaaaaaaa-0000-0000-0000-000000000004';

    v_vet1 VARCHAR2(36) := 'bbbb0005-0000-0000-0000-000000000001';
    v_vet2 VARCHAR2(36) := 'bbbb0005-0000-0000-0000-000000000002';
    v_vet3 VARCHAR2(36) := 'bbbb0005-0000-0000-0000-000000000003';

    v_service_general VARCHAR2(36);
    v_service_vacuna  VARCHAR2(36);
    v_avail_id        VARCHAR2(36);

    PROCEDURE add_appt(p_id VARCHAR2, p_cp_id VARCHAR2, p_vet_id VARCHAR2, p_service_id VARCHAR2,
                       p_status_id VARCHAR2, p_start TIMESTAMP, p_end TIMESTAMP, p_notes VARCHAR2) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM APPOINTMENTS WHERE APPOINTMENT_ID = p_id;
        IF v_count = 0 THEN
            INSERT INTO APPOINTMENTS (
                APPOINTMENT_ID, CLIENT_PET_ID, VETERINARIAN_ID, SERVICE_ID, STATUS_ID,
                AVAILABILITY_ID, SCHEDULED_START, SCHEDULED_END, NOTES, REQUESTER_PHONE_NUMBER,
                IS_PAID, CREATED_AT
            ) VALUES (
                p_id, p_cp_id, p_vet_id, p_service_id, p_status_id,
                v_avail_id, p_start, p_end, p_notes, '3109990000',
                CASE WHEN p_status_id = 'aaaaaaaa-0000-0000-0000-000000000002' THEN 1 ELSE 0 END,
                p_start - NUMTODSINTERVAL(2, 'DAY')
            );
        END IF;
    END;
BEGIN
    SELECT MIN(SERVICE_ID) INTO v_service_general FROM SERVICES WHERE UPPER(NAME) LIKE '%GENERAL%';
    SELECT MIN(SERVICE_ID) INTO v_service_vacuna  FROM SERVICES WHERE UPPER(NAME) LIKE '%VACUNA%';
    SELECT MIN(AVAILABILITY_ID) INTO v_avail_id FROM AVAILABILITIES;

    IF v_service_general IS NULL THEN SELECT MIN(SERVICE_ID) INTO v_service_general FROM SERVICES; END IF;
    IF v_service_vacuna IS NULL THEN v_service_vacuna := v_service_general; END IF;

    FOR cp IN (SELECT CLIENT_PET_ID, ROWNUM r FROM CLIENTS_PETS WHERE ROWNUM <= 12) LOOP
        -- Cita 1: Atendida en el pasado
        add_appt(
            'e1000000-0000-0000-0000-' || LPAD(TO_CHAR(cp.r * 2), 12, '0'),
            cp.CLIENT_PET_ID,
            CASE WHEN MOD(cp.r, 3) = 0 THEN v_vet1 WHEN MOD(cp.r, 3) = 1 THEN v_vet2 ELSE v_vet3 END,
            v_service_general,
            v_status_atendida,
            TO_TIMESTAMP('2026-09-15 09:00:00', 'YYYY-MM-DD HH24:MI:SS') + NUMTODSINTERVAL(cp.r, 'DAY'),
            TO_TIMESTAMP('2026-09-15 09:30:00', 'YYYY-MM-DD HH24:MI:SS') + NUMTODSINTERVAL(cp.r, 'DAY'),
            'Consulta general de valoración rutinaria'
        );

        -- Cita 2: Agendada / Cancelada
        add_appt(
            'e1000000-0000-0000-0000-' || LPAD(TO_CHAR(cp.r * 2 + 1), 12, '0'),
            cp.CLIENT_PET_ID,
            CASE WHEN MOD(cp.r, 2) = 0 THEN v_vet2 ELSE v_vet1 END,
            v_service_vacuna,
            CASE WHEN MOD(cp.r, 4) = 0 THEN v_status_cancelada WHEN MOD(cp.r, 5) = 0 THEN v_status_noasistio ELSE v_status_agendada END,
            TO_TIMESTAMP('2026-09-24 10:00:00', 'YYYY-MM-DD HH24:MI:SS') + NUMTODSINTERVAL(cp.r, 'DAY'),
            TO_TIMESTAMP('2026-09-24 10:30:00', 'YYYY-MM-DD HH24:MI:SS') + NUMTODSINTERVAL(cp.r, 'DAY'),
            'Aplicación de vacunas anuales'
        );
    END LOOP;
END;
/

-- 4. ESTANCIAS DE HOSPITALIZACIÓN (HOSPITALIZATION_STAYS)
DECLARE
    v_user_admin VARCHAR2(36) := 'bbbb0004-0000-0000-0000-000000000000';
    v_user_vet   VARCHAR2(36) := 'bbbb0004-0000-0000-0000-000000000002';

    PROCEDURE add_stay(p_id VARCHAR2, p_cp_id VARCHAR2, p_motivo VARCHAR2, p_estado NUMBER, p_days_ago NUMBER) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_count FROM HOSPITALIZATION_STAYS WHERE ID = p_id;
        IF v_count = 0 THEN
            INSERT INTO HOSPITALIZATION_STAYS (
                ID, CLIENT_PET_ID, APPOINTMENT_ID, ADMITTED_BY_USER_ID, FECHA_INGRESO, FECHA_ALTA,
                ESTADO, MOTIVO, CREATED_AT
            ) VALUES (
                p_id, p_cp_id, NULL, v_user_vet,
                SYSTIMESTAMP - NUMTODSINTERVAL(p_days_ago, 'DAY'),
                CASE WHEN p_estado = 1 THEN SYSTIMESTAMP - NUMTODSINTERVAL(p_days_ago - 2, 'DAY') ELSE NULL END,
                p_estado, p_motivo,
                SYSTIMESTAMP - NUMTODSINTERVAL(p_days_ago, 'DAY')
            );
        END IF;
    END;
BEGIN
    FOR cp IN (SELECT CLIENT_PET_ID, ROWNUM r FROM CLIENTS_PETS WHERE ROWNUM <= 8) LOOP
        add_stay(
            'f1000000-0000-0000-0000-' || LPAD(TO_CHAR(cp.r), 12, '0'),
            cp.CLIENT_PET_ID,
            CASE WHEN MOD(cp.r, 2) = 0 THEN 'Deshidratación severa y gastroenteritis aguda' ELSE 'Observación posquirúrgica y fluidoterapia' END,
            CASE WHEN cp.r <= 3 THEN 0 ELSE 1 END, -- 3 Activas, 5 Dadas de alta
            cp.r * 3
        );

        -- Insertar nota médica
        INSERT INTO HOSPITALIZATION_NOTES (ID, STAY_ID, AUTOR_USER_ID, FECHA_HORA, NOTA, ENTREGADO_A_USER_ID, CREATED_AT)
        VALUES (
            LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
            'f1000000-0000-0000-0000-' || LPAD(TO_CHAR(cp.r), 12, '0'),
            v_user_vet, SYSTIMESTAMP - NUMTODSINTERVAL(cp.r, 'DAY'),
            'Paciente estable, responde bien a la medicación intravenosa y presenta buena ingesta de alimentos.',
            v_user_admin, SYSTIMESTAMP - NUMTODSINTERVAL(cp.r, 'DAY')
        );
    END LOOP;
END;
/

-- 5. ÓRDENES MÉDICAS (MEDICAMENTOS Y PROCEDIMIENTOS PENDIENTES)
DECLARE
    v_vet_id VARCHAR2(36) := 'bbbb0005-0000-0000-0000-000000000001';
    v_appt_id VARCHAR2(36);
    v_med_id VARCHAR2(36);
    v_proc_id VARCHAR2(36);
    v_order_id VARCHAR2(36);
    v_count NUMBER;
BEGIN
    SELECT MIN(APPOINTMENT_ID) INTO v_appt_id FROM APPOINTMENTS;
    SELECT MIN(MEDICATION_ID) INTO v_med_id FROM MEDICATIONS;
    SELECT MIN(PROCEDURE_ID) INTO v_proc_id FROM PROCEDURES;

    FOR cp IN (SELECT CLIENT_PET_ID, ROWNUM r FROM CLIENTS_PETS WHERE ROWNUM <= 8) LOOP
        v_order_id := 'g1000000-0000-0000-0000-' || LPAD(TO_CHAR(cp.r * 2), 12, '0');
        SELECT COUNT(*) INTO v_count FROM MEDICATION_ORDERS WHERE MEDICATION_ORDER_ID = v_order_id;
        IF v_count = 0 THEN
            INSERT INTO MEDICATION_ORDERS (
                MEDICATION_ORDER_ID, CLIENT_PET_ID, VETERINARIAN_ID, APPOINTMENT_ID,
                IS_IN_HOUSE, REFERRED_TO, REFERRAL_REASON, STATUS, CREATED_AT
            ) VALUES (
                v_order_id, cp.CLIENT_PET_ID, v_vet_id, v_appt_id,
                1, NULL, NULL, CASE WHEN MOD(cp.r, 2) = 0 THEN 'PENDING' ELSE 'COMPLETED' END,
                SYSTIMESTAMP - NUMTODSINTERVAL(cp.r, 'DAY')
            );

            IF v_med_id IS NOT NULL THEN
                INSERT INTO MEDICATION_ORDER_ITEMS (
                    MEDICATION_ORDER_ITEM_ID, MEDICATION_ORDER_ID, MEDICATION_ID, NOTES, CREATED_AT
                ) VALUES (
                    LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    v_order_id, v_med_id, '1 tableta cada 12 horas por 7 días', SYSTIMESTAMP - NUMTODSINTERVAL(cp.r, 'DAY')
                );
            END IF;
        END IF;

        v_order_id := 'g1000000-0000-0000-0000-' || LPAD(TO_CHAR(cp.r * 2 + 1), 12, '0');
        SELECT COUNT(*) INTO v_count FROM PROCEDURE_ORDERS WHERE PROCEDURE_ORDER_ID = v_order_id;
        IF v_count = 0 THEN
            INSERT INTO PROCEDURE_ORDERS (
                PROCEDURE_ORDER_ID, CLIENT_PET_ID, VETERINARIAN_ID, APPOINTMENT_ID,
                IS_IN_HOUSE, REFERRED_TO, REFERRAL_REASON, STATUS, CREATED_AT
            ) VALUES (
                v_order_id, cp.CLIENT_PET_ID, v_vet_id, v_appt_id,
                1, NULL, NULL, 'PENDING', SYSTIMESTAMP - NUMTODSINTERVAL(cp.r, 'DAY')
            );

            IF v_proc_id IS NOT NULL THEN
                INSERT INTO PROCEDURE_ORDER_ITEMS (
                    PROCEDURE_ORDER_ITEM_ID, PROCEDURE_ORDER_ID, PROCEDURE_ID, NOTES, CREATED_AT
                ) VALUES (
                    LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
                    v_order_id, v_proc_id, 'Realizar ayuno prequirúrgico de 8 horas', SYSTIMESTAMP - NUMTODSINTERVAL(cp.r, 'DAY')
                );
            END IF;
        END IF;
    END LOOP;
END;
/

COMMIT;
