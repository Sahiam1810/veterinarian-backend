# SuperAdmin persistido y catálogos operativos

## Objetivo

Reemplazar la identidad especial de SuperAdmin configurada mediante variables de entorno por una cuenta normal persistida en Oracle y asociada a un rol de sistema protegido. Completar, además, los catálogos mínimos que necesita la operación del backend mediante seeds idempotentes y verificables.

## Estado actual

El backend reconoce al SuperAdmin mediante `SuperAdminOptions`. Su identificador, correo y hash de contraseña se cargan desde configuración, el login evita las tablas de usuarios y el JWT solo contiene `super_admin=true`. Esta identidad no tiene `USER_ACCOUNTS`, `USER_CREDENTIALS` ni refresh tokens persistidos.

La autorización dinámica omite la matriz de permisos cuando encuentra el claim especial. Varias políticas y acciones también inspeccionan ese claim directamente. Los administradores normales pueden crear o actualizar usuarios con cualquier `ROLE_ID`, y los roles sembrados actualmente no incluyen SuperAdmin.

Los seeds existentes cubren roles, módulos, permisos, cuatro estados de cita y únicamente los valores `Abierta` y `Cliente` para conversaciones. Faltan varios estados y tipos técnicos requeridos por los módulos implementados.

## Decisión de identidad

SuperAdmin será un rol persistido en `ROLES`, con identificador canónico estable. Una cuenta SuperAdmin tendrá las mismas filas y el mismo ciclo de autenticación que cualquier usuario interno:

- `USERS` conserva la persona y su `ROLE_ID`.
- `USER_ACCOUNTS` conserva usuario, correo y estado.
- `USER_CREDENTIALS` conserva exclusivamente el hash de contraseña.
- `USER_TOKENS` conserva refresh tokens protegidos y permite revocación.

El login, refresh, revocación y consulta de perfil utilizarán el flujo normal. Se eliminarán `SuperAdminOptions`, su validador, su registro de DI y las variables `SuperAdmin__*` de los ejemplos y documentación.

## Identidad técnica del rol

La autorización crítica no dependerá del nombre visible del rol. Se incorporará un identificador estable centralizado para el rol SuperAdmin. El JWT normal ya transporta `role_id` y `role`; las políticas reconocerán SuperAdmin por el identificador canónico.

El claim `super_admin=true` y el emisor especial `IssueForSuperAdmin` se retirarán. Los consumidores internos que hoy inspeccionan ese claim usarán una única extensión de claims basada en `role_id`, evitando comparaciones duplicadas.

## Invariantes de autorización

- SuperAdmin omite la matriz ordinaria de permisos y puede entrar en políticas de roles operativos.
- Solamente SuperAdmin puede administrar permisos de rol, permisos individuales, tokens administrativos y demás endpoints actualmente protegidos por `SuperAdminOnly`.
- Un usuario con permisos ordinarios sobre `Usuarios` no puede crear ni promover una cuenta a SuperAdmin.
- Un usuario con permisos ordinarios sobre `Roles` no puede modificar ni eliminar el rol de sistema SuperAdmin.
- El registro público de clientes nunca puede usar ni obtener el rol SuperAdmin.
- La promoción inicial se hace fuera de la API pública mediante una operación explícita con acceso administrativo a Oracle.

## Aprovisionamiento inicial

Se agregará un script operativo separado de los seeds. Recibirá como parámetro el correo de una cuenta interna existente y comprobará que:

1. exista exactamente una cuenta y un usuario relacionados;
2. la cuenta esté activa;
3. exista una credencial de acceso;
4. exista el rol canónico SuperAdmin.

Si las comprobaciones se cumplen, actualizará el `ROLE_ID` del usuario y revocará sus refresh tokens existentes. No contendrá correos, contraseñas ni hashes. Después de ejecutarlo, la persona deberá iniciar sesión nuevamente.

Los seeds nunca crearán cuentas personales ni credenciales.

## Catálogos incluidos

### Seguridad y permisos

- roles, incluido SuperAdmin;
- módulos del sistema;
- permisos iniciales de roles ordinarios.

SuperAdmin no necesita filas exhaustivas en `ROLE_PERMISSIONS`, porque su acceso completo es un invariante de la política. Esto evita que una edición accidental de la matriz le quite la capacidad de recuperación administrativa.

### Estados y tipos técnicos

- estados de citas;
- estados de conversaciones;
- tipos de remitente;
- tipos de mensaje;
- prioridades;
- estados de escalamiento;
- estados de ejecuciones de IA.

### Catálogos veterinarios mínimos

- tipos de servicio;
- especies iniciales;
- especialidades iniciales necesarias para operar los módulos existentes.

No se sembrarán razas extensas, diagnósticos clínicos, servicios con precios, proveedores/modelos de IA, usuarios, clientes, mascotas, citas ni historias clínicas. Esos datos son administrativos, variables por clínica o transaccionales.

## Estrategia de seeds

- Oracle SQL ejecutable con SQL*Plus o SQL Developer.
- Operaciones `MERGE` idempotentes.
- GUID canónicos y estables para referencias técnicas.
- Coincidencias y validaciones que detecten conflictos entre nombres canónicos e identificadores existentes.
- `COMMIT` explícito por script.
- Orden único documentado y script maestro de ejecución.
- Consultas de verificación con conteos y valores esperados.
- `cleanup_seeds.sql` permanece separado y nunca se invoca desde el ejecutor normal.

## Compatibilidad y migración

No se modifica el esquema ni se requiere una migración EF Core: SuperAdmin utiliza las tablas y relaciones existentes. El contrato HTTP de login, refresh y `/me` conserva su forma. Cambia el comportamiento de la cuenta anteriormente configurada: deberá existir en base de datos y ser promovida explícitamente.

Los tokens antiguos con `super_admin=true` dejarán de ser suficientes después del despliegue. El cambio exige cerrar sesión y obtener un JWT nuevo. Esta invalidación es intencional y evita mantener dos autoridades paralelas.

## Capas afectadas

| Área | Impacto |
|---|---|
| Domain | Constante/identidad técnica del rol de sistema y protección de su ciclo de vida. |
| Application | Reglas que impiden asignar, modificar o eliminar SuperAdmin desde operaciones ordinarias. |
| Infrastructure | Eliminación de opciones especiales y unificación del flujo de autenticación/JWT. |
| API | Políticas y utilidades de claims basadas en `role_id`. |
| Database | Seeds idempotentes, ejecutor, verificación y script de aprovisionamiento. |
| Tests/docs | Pruebas focalizadas de autenticación/autorización y documentación de ejecución. |

## Verificación

- Compilar la solución completa.
- Ejecutar solamente las pruebas focalizadas de autenticación, JWT, políticas, permisos, usuarios y roles durante el desarrollo.
- Validar estáticamente los scripts y, únicamente con autorización explícita, ejecutarlos contra una base Oracle concreta.
- Confirmar que no quedan referencias de runtime a `SuperAdminOptions`, `SuperAdmin__*` o `super_admin=true`.
- Ejecutar `git diff --check` y revisar que no se incluyan secretos ni archivos del chatbot.
