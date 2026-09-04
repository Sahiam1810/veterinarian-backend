-- Matriz inicial de permisos por rol y módulo.
-- Requiere roles_seed.sql y modules_seed.sql.
-- Solo inserta permisos faltantes: no sobrescribe ajustes administrativos existentes.
SET DEFINE OFF;

DECLARE
    PROCEDURE ensure_permission(
        p_id VARCHAR2,
        p_role_id VARCHAR2,
        p_module_name VARCHAR2,
        p_can_view NUMBER,
        p_can_create NUMBER,
        p_can_edit NUMBER,
        p_can_delete NUMBER) IS
    BEGIN
        MERGE INTO ROLE_PERMISSIONS target
        USING (
            SELECT p_id ID,
                   p_role_id ROLE_ID,
                   (SELECT MODULE_ID FROM MODULES WHERE NAME = p_module_name) MODULE_ID,
                   p_can_view CAN_VIEW,
                   p_can_create CAN_CREATE,
                   p_can_edit CAN_EDIT,
                   p_can_delete CAN_DELETE
            FROM DUAL
        ) source
        ON (target.ROLE_ID = source.ROLE_ID AND target.MODULE_ID = source.MODULE_ID)
        WHEN NOT MATCHED THEN
            INSERT (
                ROLE_PERMISSION_ID, ROLE_ID, MODULE_ID,
                CAN_VIEW, CAN_CREATE, CAN_EDIT, CAN_DELETE, CREATED_AT)
            VALUES (
                source.ID, source.ROLE_ID, source.MODULE_ID,
                source.CAN_VIEW, source.CAN_CREATE,
                source.CAN_EDIT, source.CAN_DELETE, SYSTIMESTAMP);
    END;
BEGIN
    -- Administrador
    ensure_permission('a4b4bb3e-3516-4b6c-a577-3ec4d65c3a7b', '11111111-1111-1111-1111-111111111111', 'Clientes', 1, 1, 1, 1);
    ensure_permission('7f3fdc4f-b845-4403-aa3c-209fb0d14968', '11111111-1111-1111-1111-111111111111', 'Mascotas', 1, 1, 1, 1);
    ensure_permission('9dc06609-4a2f-4980-8d3b-498e3e9ef622', '11111111-1111-1111-1111-111111111111', 'Especies y Razas', 1, 1, 1, 1);
    ensure_permission('204086ef-1ac5-4c4a-86d9-0ab0125b617e', '11111111-1111-1111-1111-111111111111', 'Especialidades', 1, 1, 1, 1);
    ensure_permission('b95efcce-cc93-4474-be52-2676bc132f73', '11111111-1111-1111-1111-111111111111', 'Veterinarios', 1, 1, 1, 1);
    ensure_permission('c89cdbba-e1f7-4ff8-becd-c39ac914475e', '11111111-1111-1111-1111-111111111111', 'Citas', 1, 1, 1, 1);
    ensure_permission('3b9dde6d-68e1-4470-b82c-327feb02f4f6', '11111111-1111-1111-1111-111111111111', 'Historiales Clínicos', 1, 1, 1, 1);
    ensure_permission('d704a878-f7a3-4fa7-b20a-3450292720a7', '11111111-1111-1111-1111-111111111111', 'Servicios', 1, 1, 1, 1);
    ensure_permission('ab64891f-bba9-4e1f-8901-8aa225643550', '11111111-1111-1111-1111-111111111111', 'Estados de Cita', 1, 1, 1, 1);
    ensure_permission('4ac22e89-a9a5-4576-8c80-97c000dddf12', '11111111-1111-1111-1111-111111111111', 'Cuentas y Pagos', 1, 1, 1, 1);
    ensure_permission('73ae229d-f977-46f1-bfd5-42c7e92731f9', '11111111-1111-1111-1111-111111111111', 'Notificaciones', 1, 0, 0, 1);
    ensure_permission('07a733f3-3a97-42be-82d7-7aeb39366eca', '11111111-1111-1111-1111-111111111111', 'Usuarios', 1, 1, 1, 1);
    ensure_permission('7fb38b70-ac2f-4998-b64a-a769f27fdf7b', '11111111-1111-1111-1111-111111111111', 'Roles', 1, 1, 1, 1);

    -- Veterinario
    ensure_permission('0f5fbe54-b049-480d-8a54-1cc6e5bace30', '44444444-4444-4444-4444-444444444444', 'Clientes', 1, 0, 0, 0);
    ensure_permission('13b24e43-926a-4680-adee-0230ddb26c79', '44444444-4444-4444-4444-444444444444', 'Mascotas', 1, 0, 0, 0);
    ensure_permission('79093a43-37c7-414b-8795-f8cd6873eb7b', '44444444-4444-4444-4444-444444444444', 'Especies y Razas', 1, 0, 0, 0);
    ensure_permission('97dfa9c2-1cb2-4294-9f14-a6a226817f8c', '44444444-4444-4444-4444-444444444444', 'Especialidades', 1, 0, 0, 0);
    ensure_permission('adca595a-367f-45a5-8100-bd61f316bbc5', '44444444-4444-4444-4444-444444444444', 'Veterinarios', 1, 0, 0, 0);
    ensure_permission('017ae40b-1d25-4e20-984a-258e0e2f1fe5', '44444444-4444-4444-4444-444444444444', 'Citas', 1, 0, 1, 0);
    ensure_permission('1ab5c377-ad36-48db-bd3b-db02eecf2d64', '44444444-4444-4444-4444-444444444444', 'Historiales Clínicos', 1, 1, 1, 0);
    ensure_permission('80f53cfa-0ecf-42aa-a9d7-38731f00f599', '44444444-4444-4444-4444-444444444444', 'Servicios', 1, 0, 0, 0);
    ensure_permission('bc33442d-6bee-4b34-87b6-4e521551c8a7', '44444444-4444-4444-4444-444444444444', 'Estados de Cita', 1, 0, 0, 0);

    -- Recepcionista
    ensure_permission('76be45ca-8349-410a-ab5c-ce4825bef0e0', '55555555-5555-5555-5555-555555555555', 'Clientes', 1, 1, 1, 0);
    ensure_permission('4d7f8f54-0364-4afb-8049-121136dc5595', '55555555-5555-5555-5555-555555555555', 'Mascotas', 1, 1, 1, 0);
    ensure_permission('1955bd98-c522-4377-84d1-9b1f2fec7d7b', '55555555-5555-5555-5555-555555555555', 'Especies y Razas', 1, 0, 0, 0);
    ensure_permission('57d716b1-2c4e-4949-8cda-276d1d8d0cb4', '55555555-5555-5555-5555-555555555555', 'Especialidades', 1, 0, 0, 0);
    ensure_permission('9a6c1378-c3b5-414f-a7ce-49e1ed565a8d', '55555555-5555-5555-5555-555555555555', 'Veterinarios', 1, 0, 0, 0);
    ensure_permission('d0e6df49-4072-4159-9b1c-54fbb6866b57', '55555555-5555-5555-5555-555555555555', 'Citas', 1, 1, 1, 1);
    ensure_permission('2f85e7b7-1b66-4c71-8195-75dd77a5cf7c', '55555555-5555-5555-5555-555555555555', 'Historiales Clínicos', 1, 0, 0, 0);
    ensure_permission('3642d164-4871-4982-b679-27fe48cd3e38', '55555555-5555-5555-5555-555555555555', 'Servicios', 1, 0, 0, 0);
    ensure_permission('73869746-0660-4d14-b298-e11b28258231', '55555555-5555-5555-5555-555555555555', 'Estados de Cita', 1, 0, 0, 0);
    ensure_permission('352fc580-3c6f-4ab4-b491-8a535e21b0d6', '55555555-5555-5555-5555-555555555555', 'Cuentas y Pagos', 1, 1, 0, 0);

    -- Auxiliar
    ensure_permission('b7540f8f-7ac3-4479-a46e-b0efc34d588c', '66666666-6666-6666-6666-666666666666', 'Clientes', 1, 0, 0, 0);
    ensure_permission('72686e74-29b0-46f6-b4bf-63fd6430ada9', '66666666-6666-6666-6666-666666666666', 'Mascotas', 1, 0, 0, 0);
    ensure_permission('86aef2cc-1b8b-4dd3-bffa-3ca04e4b5fcd', '66666666-6666-6666-6666-666666666666', 'Especies y Razas', 1, 0, 0, 0);
    ensure_permission('f0088433-eb22-419c-979d-900114049b96', '66666666-6666-6666-6666-666666666666', 'Especialidades', 1, 0, 0, 0);
    ensure_permission('83b9375a-6dff-4ef7-b727-53e648364ba6', '66666666-6666-6666-6666-666666666666', 'Citas', 1, 0, 0, 0);
    ensure_permission('8fee0f45-2c62-4aef-aae8-63a59df078a6', '66666666-6666-6666-6666-666666666666', 'Historiales Clínicos', 1, 0, 0, 0);
    ensure_permission('137f09ee-ac41-4abe-aff8-ba1306281c33', '66666666-6666-6666-6666-666666666666', 'Servicios', 1, 0, 0, 0);
    ensure_permission('d8bdcce9-0696-4f40-9828-193708deb19c', '66666666-6666-6666-6666-666666666666', 'Estados de Cita', 1, 0, 0, 0);

    -- Cliente: consulta de sus propios datos por medio del chatbot.
    ensure_permission('c7e2a91b-4d3f-4a8e-9b1c-2f6d8e0a5c47', '77777777-7777-7777-7777-777777777777', 'Clientes', 1, 0, 0, 0);
    ensure_permission('f0c1f642-fe85-4381-a129-56e7ce82ab1d', '77777777-7777-7777-7777-777777777777', 'Mascotas', 1, 0, 0, 0);
    ensure_permission('390421e3-3ad7-4eaa-90a3-20a9715b2e2f', '77777777-7777-7777-7777-777777777777', 'Citas', 1, 0, 0, 0);
    ensure_permission('c6882d60-a8fd-4886-a5f9-bfd1a9d3cf4a', '77777777-7777-7777-7777-777777777777', 'Historiales Clínicos', 1, 0, 0, 0);
END;
/

COMMIT;
