# Reset controlado de datos de prueba

Artefactos versionados para vaciar los datos de prueba de la base Oracle de
Huellitas y dejarla lista para que catalogos comerciales y usuarios se creen
desde la aplicacion. **No se ejecutan automaticamente**: cada ejecucion es
manual, con aprobacion explicita y siguiendo este documento.

> **Estado: NO aprobado para ejecutar en Produccion.** Pendientes: instancia
> Oracle temporal externa, PR aprobado, Data Pump, y prueba de reset y
> rollback en la instancia temporal (secciones 4 y 7). La inclusion de la
> tabla manual historica `CLIENTS_PHONE_FIX_20260925` esta aprobada solo como
> diseno y cambio local; **no autoriza ninguna ejecucion contra Produccion**.

| Archivo | Tipo | DML | Salida |
|---|---|---|---|
| `controlled_test_data_reset_precheck.sql` | Solo lectura | No | `0` = `READY`, `3` = `NOT_READY` |
| `controlled_test_data_reset.sql` | Reset transaccional | Solo `DELETE` + una confirmacion final | `0` = `RESET_SUCCESS` o `ALREADY_RESET`, distinto de `0` = abortado con rollback |
| `controlled_test_data_reset_postcheck.sql` | Solo lectura | No | `0` = `RESET_SUCCESS`, `4` = `POSTCHECK_FAILED` |

## 1. Proposito y alcance

- Eliminar todos los datos operativos y de prueba: clientes, mascotas, citas,
  historia clinica, ordenes, hospitalizaciones, notificaciones, chat,
  Telegram, OTP, staff de prueba, perfiles veterinarios, AgentHumans,
  disponibilidades y ausencias.
- Vaciar catalogos comerciales: `SERVICES`, `MEDICATIONS`, `PROCEDURES`,
  `SUPPLIES`.
- Eliminar el rol `Cliente` (`77777777-7777-7777-7777-777777777777`), sus
  usuarios y sus permisos.
- Vaciar la tabla manual historica `CLIENTS_PHONE_FIX_20260925` (fuera del
  modelo EF; seccion 2.4).
- Conservar el esquema, las migraciones, los catalogos estructurales, los
  roles validos con sus permisos y **exactamente un SuperAdmin**.

Aprobado por la lider con las condiciones:
Data Pump obligatorio, prueba previa en una instancia Oracle temporal externa y ejecucion en
Produccion solo despues de validar precheck, FKs, rollback y postcheck.

## 2. Tablas

Inventario exigido en los tres scripts:

```text
46 USER_TABLES = 44 tablas del modelo EF
               + __EFMigrationsHistory
               + CLIENTS_PHONE_FIX_20260925 (tabla manual historica)
35 tablas afectadas por DELETE
31 tablas deben terminar en 0
 4 tablas parcialmente preservadas: AGENT_HUMANS, USERS, ROLES, ROLE_PERMISSIONS
11 tablas preservadas sin DELETE (seccion 2.1)
```

Si falta cualquier tabla esperada, o hay alguna adicional, el precheck queda
en `NOT_READY`, el reset falla antes de cualquier `DELETE` y el postcheck
falla. Si la tabla faltante tiene un `DELETE` estatico en el reset (como
`CLIENTS_PHONE_FIX_20260925`), el fallo es `ORA-00942` al compilar el bloque,
no un codigo de guarda (seccion 3.4).

### 2.1 Tablas preservadas (sin DELETE)

| Tabla | Condicion validada |
|---|---|
| `__EFMigrationsHistory` | 78 migraciones; ultima `20260928135138_AddHospitalizationSettings` |
| `MODULES` | Mismo conteo (y huella dentro del reset) |
| `STATUS_APPOINTMENTS` | No vacia; nombres `AGENDADA`, `ATENDIDA`, `CONFIRMADA`, `EN_PROGRESO`, `CANCELADA`, `NO_ASISTIO` |
| `SENDER_TYPES` | IDs fijos `82000000-...-000000000001..4` (incluye el tipo de emisor "Cliente", que es catalogo legitimo) |
| `ESCALATIONS_STATUSES` | IDs fijos `85000000-...-000000000001..5` |
| `SPECIES`, `RACES`, `SPECIALTIES`, `TYPE_SERVICES`, `DIAGNOSTICS` | No vacias; mismo conteo (y huella dentro del reset) |
| `HOSPITALIZATION_SETTINGS` | Debe estar en `0` antes y despues (la tarifa no se configura aqui) |

### 2.2 Tablas con borrado parcial

| Tabla | Se conserva |
|---|---|
| `USERS` | Solo el SuperAdmin, identificado por `ROLE_ID = 99999999-9999-9999-9999-999999999999` y guardas de conteo (nunca por nombre ni correo) |
| `AGENT_HUMANS` | La fila del SuperAdmin si tiene exactamente una; si no tiene, se borran todas; si tiene mas de una, el reset aborta |
| `ROLE_PERMISSIONS` | Todos los permisos de SuperAdmin, Administrador, Veterinario, Recepcionista y Auxiliar; solo se borran los del rol Cliente |
| `ROLES` | SuperAdmin, Administrador, Veterinario, Recepcionista y Auxiliar; solo se borra Cliente, despues de quitar sus usuarios y permisos |

### 2.2.1 Criterio final aprobado de las tablas parciales

```text
USERS            = 1, y es el SuperAdmin activo.
ROLES            = 5, y son exactamente SuperAdmin, Administrador,
                   Veterinario, Recepcionista y Auxiliar.
ROLE_PERMISSIONS = permisos actuales de esos cinco roles;
                   no se resiembran ni se modifican.
ROLES con NAME = Cliente (y el ID fijo de Cliente) = 0
USERS con el ROLE_ID de Cliente                    = 0
ROLE_PERMISSIONS del rol Cliente                   = 0
```

- El precheck y la fase de guardas del reset imprimen `PLANNED_KEEP[...]` y
  abortan si el estado final planificado no es USERS = 1, ROLES = 5 y
  ROLE_PERMISSIONS = permisos de los cinco roles aprobados.
- El postcheck del reset (misma sesion) compara ademas la huella de esos
  permisos antes y despues: cualquier cambio aborta con rollback.
- El frontend puede mostrar solo cuatro plantillas de rol porque oculta
  SuperAdmin como rol tecnico del sistema. Eso **no** cambia el criterio de
  base de datos: en Oracle quedan exactamente cinco roles.

### 2.3 Tablas vaciadas, en este orden exacto

1. `CLIENTS_PHONE_FIX_20260925` (tabla manual historica, fuera de EF)
2. `NOTIFICATIONS`
3. `VACCINATIONS`
4. `MEDICAL_RECORDS`
5. `MEDICATION_ORDER_ITEMS`
6. `PROCEDURE_ORDER_ITEMS`
7. `MEDICATION_ORDERS`
8. `PROCEDURE_ORDERS`
9. `SUPPLY_CONSUMPTIONS`
10. `HOSPITALIZATION_NOTES`
11. `HOSPITALIZATION_STAYS`
12. `APPOINTMENT_STATUS_HISTORIES`
13. `APPOINTMENTS`
14. `CHAT_MESSAGES`
15. `CHAT_ESCALATIONS`
16. `CHAT_PARTICIPANTS`
17. `TELEGRAM_USER_LINKS`
18. `CHAT_CONVERSATIONS`
19. `TELEGRAM_INBOUND_UPDATES`
20. `CONTACT_VERIFICATION_SESSIONS`
21. `CLIENTS_PETS`
22. `PETS`
23. `CLIENTS`
24. `VETERINARIAN_ABSENCES`
25. `AVAILABILITIES`
26. `VETERINARIANS`
27. `AGENT_HUMANS` (parcial)
28. `USER_TOKENS`
29. `USERS` (parcial)
30. `ROLE_PERMISSIONS` (parcial)
31. `ROLES` (parcial)
32. `SERVICES`
33. `MEDICATIONS`
34. `PROCEDURES`
35. `SUPPLIES`

El orden borra hijas antes que padres para las 57 FKs esperadas del modelo EF.
La tabla manual historica va primera: ninguna FK esperada la referencia ni
sale de ella, y cualquier FK que la involucre se trata como inesperada y hace
abortar. Las FKs con `ON DELETE CASCADE` no afectan filas adicionales porque
sus hijas ya estan vacias; cada `DELETE` se compara con el conteo planificado
y cualquier diferencia aborta con rollback.

### 2.4 Tabla manual historica `CLIENTS_PHONE_FIX_20260925`

- Es una tabla manual historica, **fuera del modelo oficial**: no forma parte
  del modelo EF, las migraciones ni el snapshot.
- **No debe agregarse** a backend, frontend, EF, migraciones, endpoints ni
  flujos operativos. Estos scripts no crean ni modifican su estructura
  (sin PK, FK, indices, triggers ni migraciones).
- Contiene PII de prueba (6 filas esperadas) y se vacia **solo** como parte de
  este reset especifico.
- **Debe existir obligatoriamente** en Produccion y en la instancia temporal
  restaurada desde Data Pump. Si falta:
  - precheck: `AUX_TABLE_MISSING` y `PRECHECK_RESULT=NOT_READY`;
  - reset: falla con `ORA-00942` al compilar el bloque, antes de cualquier
    `DELETE` (seccion 3.4);
  - postcheck: `AUX_TABLE_MISSING` como fallo.
- Validacion de estructura (precheck y reset): exactamente una columna
  `FIXED_AT` de tipo **`TIMESTAMP WITH TIME ZONE`** (en Oracle,
  `TIMESTAMP(6) WITH TIME ZONE`); ninguna fila con `FIXED_AT IS NULL`; y la
  guarda UTC debe poder ejecutarse. No se exige `CREATED_AT` (la tabla no lo
  tiene).
- **No se aceptan `DATE`, `TIMESTAMP` sin zona horaria ni
  `TIMESTAMP WITH LOCAL TIME ZONE`**: el tipo con zona horaria explicita
  permite evaluar el corte en UTC sin depender de la zona horaria de la
  sesion. Cualquier otro tipo produce `AUX_FIXED_AT_TYPE_UNSUPPORTED`
  (precheck `NOT_READY`; reset abortado con `-20024` antes de cualquier
  `DELETE`).
- Guarda de corte: `SYS_EXTRACT_UTC(FIXED_AT) > corte_utc`. Una fila posterior
  al corte o con `FIXED_AT` nulo deja el precheck en `NOT_READY` y aborta el
  reset antes de cualquier `DELETE`.
- Tope inicial: `6`. Se **recaptura durante la ventana de mantenimiento** con
  el conteo real del precheck; si cambia, se actualiza el tope en precheck y
  reset mediante un cambio revisado.
- Se borra en el mismo bloque transaccional, antes de cualquier otro
  `DELETE`, con verificacion de `SQL%ROWCOUNT` y sin confirmaciones
  adicionales.
- Postcheck final: `CLIENTS_PHONE_FIX_20260925 = 0`.
- **Recuperacion**: tras el reset, su contenido solo puede recuperarse
  restaurando el Data Pump del esquema (seccion 6.3).

## 3. Garantias de los scripts

- Compatibles con SQL*Plus; `WHENEVER OSERROR EXIT FAILURE ROLLBACK`,
  `WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK`, `SET ECHO OFF`,
  `SET VERIFY OFF`, `SET FEEDBACK ON`, `SET AUTOCOMMIT OFF`,
  `SET EXITCOMMIT OFF`.
- Sin DDL ni sentencias de modificacion distintas de `DELETE`. El reset tiene
  una sola confirmacion transaccional, al final.
- Una sola transaccion: guardas, `DELETE` ordenados, postcheck en la misma
  sesion y confirmacion solo si todo pasa. Cualquier fallo provoca rollback y
  salida distinta de cero.
- Si no hay nada que borrar, imprime `RESET_RESULT=ALREADY_RESET` y sale `0`
  sin modificar datos.
- No imprime filas, IDs, correos, telefonos, hashes, tokens ni secretos:
  solo conteos, nombres de tablas y constraints, y codigos de razon.
- No escribe logs en tablas. El operador captura la salida con un spool
  externo con permisos `600`.
- `DBMS_APPLICATION_INFO.SET_MODULE('HUELLITAS_TEST_DATA_RESET', <accion>)`
  con las acciones `PRECHECK`, `GUARDS`, `DELETE`, `POSTCHECK`, `CONFIRM`.

### 3.1 Corte UTC

- El reset captura el corte una sola vez con `SYSTIMESTAMP AT TIME ZONE 'UTC'`
  y lo imprime como `RESET_CUTOFF_UTC=<ISO-8601 UTC>`. No se acepta por
  parametro.
- Cuenta filas posteriores al corte en las 35 tablas afectadas y aborta si
  hay alguna: `CREATED_AT > corte` en las 34 tablas del modelo EF y
  `SYS_EXTRACT_UTC(FIXED_AT) > corte` en `CLIENTS_PHONE_FIX_20260925`.
- **El corte no impide escrituras concurrentes**: una fila creada despues de la
  captura y antes del borrado no se detecta de forma fiable. Por eso Backend
  y ChatBot deben estar detenidos durante toda la ventana.
- El precheck imprime `PRECHECK_CUTOFF_UTC=` solo como referencia; el corte
  valido es el del reset.

### 3.2 Guardas del reset (codigo de error, si falla)

| Codigo | Motivo |
|---|---|
| `-20006` | Inventario interno del script inconsistente (no son 46 tablas esperadas o 35 DELETE) |
| `-20005` | Parametros ausentes o con formato invalido |
| `-20001` | Frase de confirmacion distinta de `RESET_TEST_DATA_CONFIRMED` |
| `-20002` | PDB distinta de `SYS_CONTEXT('USERENV','CON_NAME')` |
| `-20003` | Esquema distinto de `SYS_CONTEXT('USERENV','CURRENT_SCHEMA')` |
| `-20004` | Usuario Oracle (`SESSION_USER`) distinto del esquema esperado |
| `-20010` | Migraciones distintas de 78 o ultima migracion distinta |
| `-20023` | Tablas esperadas faltantes detectables en ejecucion. No aplica a tablas con `DELETE` estatico, como `CLIENTS_PHONE_FIX_20260925`: su ausencia impide compilar el bloque (`ORA-00942`, seccion 3.4) |
| `-20016` | Tablas inesperadas, o `USER_TABLES` distinto de 46 |
| `-20024` | `CLIENTS_PHONE_FIX_20260925` sin columna `FIXED_AT`, con `FIXED_AT` distinto de `TIMESTAMP WITH TIME ZONE`, o con guarda UTC no ejecutable |
| `-20025` | Filas de `CLIENTS_PHONE_FIX_20260925` con `FIXED_AT IS NULL` |
| `-20017` | FKs inesperadas, faltantes o que violan el orden de borrado |
| `-20018` | Constraints no `ENABLED/VALIDATED` o indices no validos |
| `-20015` | Objetos invalidos |
| `-20011` | Roles faltantes, renombrados o inesperados; nombre Cliente en un ID inesperado; estado final planificado distinto de ROLES = 5 o de los permisos de los cinco roles aprobados |
| `-20012` | No hay exactamente un SuperAdmin activo con hash de contrasena, o el estado final planificado no es USERS = 1 |
| `-20013` | Catalogos estructurales vacios o sin IDs/nombres fijos |
| `-20022` | `HOSPITALIZATION_SETTINGS` no vacia |
| `-20021` | Mas de un `AGENT_HUMANS` para el SuperAdmin |
| `-20020` | Tope de volumen excedido |
| `-20019` | Filas creadas despues del corte |
| `-20030` | Un `DELETE` afecto un numero de filas distinto del planificado |
| `-20040` | Postcheck en la misma sesion fallido |

Las guardas de parametros y entorno abortan de inmediato; el resto se acumula,
se imprime como `NOT_READY_REASON=...` y aborta con el primer codigo. El
mensaje del error incluye todas las razones por si `DBMS_OUTPUT` no llega a
mostrarse al abortar.

En Linux, el codigo de salida del proceso es el numero de error modulo 256
(por ejemplo, `-20001` sale como `33`). Solo importa que sea distinto de
cero; el motivo exacto esta en el spool.

### 3.3 Topes de volumen (pendientes de aprobacion)

| Tabla | Tope |
|---|---|
| `CLIENTS_PHONE_FIX_20260925` | 6 (tope inicial; se recaptura durante la ventana de mantenimiento) |
| `CHAT_MESSAGES`, `TELEGRAM_INBOUND_UPDATES` | 200000 |
| `APPOINTMENT_STATUS_HISTORIES`, `USER_TOKENS` | 100000 |
| `NOTIFICATIONS`, `MEDICATION_ORDER_ITEMS`, `PROCEDURE_ORDER_ITEMS`, `SUPPLY_CONSUMPTIONS`, `HOSPITALIZATION_NOTES`, `APPOINTMENTS`, `CHAT_PARTICIPANTS`, `CONTACT_VERIFICATION_SESSIONS` | 50000 |
| `VACCINATIONS`, `MEDICAL_RECORDS`, `MEDICATION_ORDERS`, `PROCEDURE_ORDERS`, `CHAT_ESCALATIONS`, `CHAT_CONVERSATIONS`, `CLIENTS_PETS`, `PETS`, `CLIENTS`, `AVAILABILITIES` | 20000 |
| `HOSPITALIZATION_STAYS`, `TELEGRAM_USER_LINKS`, `VETERINARIAN_ABSENCES` | 10000 |
| `MEDICATIONS`, `PROCEDURES`, `SUPPLIES` | 5000 |
| `ROLE_PERMISSIONS`, `SERVICES` | 2000 |
| `USERS` | 1000 |
| `VETERINARIANS`, `AGENT_HUMANS` | 500 |
| `ROLES` | 20 |

Los topes estan duplicados en el precheck y en el reset (`init_caps`) y deben
mantenerse identicos.

### 3.4 Tabla auxiliar ausente (fallo seguro con ORA-00942)

`CLIENTS_PHONE_FIX_20260925` es obligatoria y el reset la borra con un
`DELETE` estatico, sin SQL dinamico. Por eso, si la tabla no existe, Oracle
puede rechazar el bloque PL/SQL con `ORA-00942` (acompanado de `ORA-06550`)
durante el parseo o la compilacion, antes de ejecutar ninguna guarda y
antes de cualquier `DELETE`.

- Es un **fallo seguro**: no se ejecuta ninguna sentencia de modificacion,
  `WHENEVER SQLERROR` hace rollback y SQL*Plus sale con codigo distinto de
  cero.
- **No equivale** al codigo de guarda `-20023`: el reset no puede emitir
  `-20023` ni `AUX_TABLE_MISSING` en este caso, porque el bloque no llega a
  ejecutarse. Tampoco aparecen lineas de `DBMS_OUTPUT` del reset.
- El precheck (`AUX_TABLE_MISSING`, `NOT_READY`) y el postcheck
  (`AUX_TABLE_MISSING`, fallo) si informan la ausencia, porque no contienen
  SQL estatico sobre la tabla.
- La instancia temporal restaurada desde el Data Pump debe validar este
  comportamiento (seccion 7) antes de considerar aprobado el runbook.

## 4. Prerrequisitos (todos obligatorios)

1. Aprobacion escrita de la lider para el entorno concreto (instancia
   temporal o Produccion) y la ventana.
2. PR con estos artefactos revisado y aprobado; se ejecuta solo la version
   aprobada.
3. Prueba completa de reset y rollback en una instancia Oracle temporal
   externa (seccion 7), documentada y aprobada antes de planificar
   Produccion.
4. Ventana de mantenimiento comunicada.
5. Export Data Pump del esquema completo, **probado** mediante una
   importacion en la instancia temporal (seccion 6).
6. Backend y ChatBot **detenidos** (sin escrituras ni jobs en segundo plano).
7. Sin updates de Telegram pendientes de procesar (seccion 9.1).
8. Precheck en `READY` en el mismo entorno y la misma ventana.
9. Revision de que la rama y los scripts usados coinciden con la version
   aprobada (hash del commit anotado en el registro de la ventana).

## 5. Procedimiento

Todos los valores entre `<...>` son placeholders. No escribir contrasenas ni
cadenas de conexion en la linea de comandos, en archivos versionados ni en el
historial de la shell.

### 5.1 Preparar la sesion y el spool

```bash
umask 077
mkdir -p <DIRECTORIO_LOGS_PRIVADO>
cd <RUTA_REPO_BACKEND>/database/admin/reset
sqlplus -L /nolog
```

Dentro de SQL*Plus:

```sql
CONNECT <USUARIO_ESQUEMA>@<ALIAS_TNS>
SPOOL <DIRECTORIO_LOGS_PRIVADO>/reset_<ENTORNO>_<FECHA>.log
```

`CONNECT` pide la contrasena de forma interactiva. Verificar despues que el
spool tiene permisos `600` (`ls -l`) y guardarlo fuera del repositorio.

### 5.2 Precheck

```sql
@controlled_test_data_reset_precheck.sql <PDB_ESPERADA> <ESQUEMA_ESPERADO>
```

Revisar en el spool:

- `PRECHECK_RESULT=READY` y `NOT_READY_REASONS_TOTAL=0`.
- `DATA_STATE=PENDING_RESET` (o `ALREADY_RESET` si no hay nada que borrar).
- `INVENTORY_EXPECTED_TABLES=46 DELETE_TABLES=35 ZERO_TABLES=31 PARTIAL_TABLES=4`
  y `SCHEMA_USER_TABLES=46 EXPECTED=46`.
- `AUX_TABLE_PRESENT=1`, `AUX_FIXED_AT_COLUMN=1`,
  `AUX_FIXED_AT_TYPE=TIMESTAMP(6) WITH TIME ZONE`, `AUX_FIXED_AT_NULL_ROWS=0`,
  `AUX_UTC_GUARD_EXECUTABLE=YES` y
  `TABLE_COUNT[CLIENTS_PHONE_FIX_20260925]` dentro de su `CAP` (anotar el
  conteo real para recapturar el tope).
- `FK_EXPECTED_PAIRS_FOUND=57/57`, `FK_UNEXPECTED=0`, `FK_ORDER_VIOLATIONS=0`.
- `CONSTRAINTS_NOT_ENABLED_VALIDATED=0`, `INDEXES_NOT_VALID=0`,
  `INVALID_OBJECTS=0`.
- `SUPERADMIN_USERS=1`, `SUPERADMIN_VALID=1`,
  `AGENT_HUMANS_LINKED_TO_SUPERADMIN` en `0` o `1`.
- `PLANNED_KEEP[USERS]=1`, `PLANNED_KEEP[ROLES]=5` y
  `PLANNED_KEEP[ROLE_PERMISSIONS]` igual a su `EXPECTED` (permisos de los
  cinco roles aprobados).
- `TABLE_COUNT[...]` por debajo de su `CAP`.
- Anotar el valor de `BASELINE_COUNTS=` para el postcheck.

Si el resultado es `NOT_READY`, **detener** y resolver cada
`NOT_READY_REASON` con la lider antes de continuar. En particular,
`AUX_TABLE_MISSING` o `AUX_FIXED_AT_TYPE_UNSUPPORTED` bloquean el reset.

### 5.3 Backup Data Pump (inmediatamente antes del reset)

Ver seccion 6.

### 5.4 Reset

```sql
@controlled_test_data_reset.sql RESET_TEST_DATA_CONFIRMED <PDB_ESPERADA> <ESQUEMA_ESPERADO>
```

Resultado esperado en el spool:

- `RESET_CUTOFF_UTC=...`
- `RESET_READINESS=READY`
- `PLANNED_DELETE[...]` y `DELETED[...]` con los mismos valores en las 35
  tablas, empezando por `DELETED[CLIENTS_PHONE_FIX_20260925]`.
- `POST_ZERO_TABLES_OK=31/31` y `POSTCHECK_FAILURES=0`
- `RESET_RESULT=RESET_SUCCESS` (o `RESET_RESULT=ALREADY_RESET`).

SQL*Plus termina al final del script. Cualquier otra salida implica que la
transaccion se revirtio y los datos no cambiaron. Si el spool muestra
`ORA-00942` sin lineas `RESET_*`, falta una tabla con `DELETE` estatico
(normalmente `CLIENTS_PHONE_FIX_20260925`): el bloque no compilo, no se
borro nada y hay que volver al precheck (seccion 3.4).

### 5.5 Postcheck independiente

En una sesion nueva (con su propio spool):

```sql
@controlled_test_data_reset_postcheck.sql <PDB_ESPERADA> <ESQUEMA_ESPERADO> <BASELINE_COUNTS>
```

Debe imprimir `POSTCHECK_RESULT=RESET_SUCCESS` y salir con `0`. Valida:

- 78 migraciones y la ultima correcta.
- `ROLES_TOTAL=5`, `ROLES_UNEXPECTED=0` y los cinco roles aprobados por ID y
  nombre.
- Rol Cliente en `0` por ID (`CLIENT_ROLE=0`) y por nombre
  (`ROLES_NAMED_CLIENTE=0`); `CLIENT_USERS=0`; `CLIENT_PERMISSIONS=0`.
- `ROLE_PERMISSIONS_TOTAL` igual a los permisos de los cinco roles aprobados,
  con el mismo conteo que en `BASELINE_COUNTS`.
- `USERS_TOTAL=1` y un SuperAdmin activo con hash.
- `AGENT_HUMANS` en `0`, o `1` perteneciente al SuperAdmin.
- `AUX_TABLE_PRESENT=1 ROWS=0 EXPECTED=0` (`CLIENTS_PHONE_FIX_20260925 = 0`).
  Si la tabla falta, imprime `AUX_TABLE_PRESENT=0` y falla con
  `AUX_TABLE_MISSING`.
- `ZERO_TABLES_OK=31/31` (las 31 tablas no parciales en `0`) y
  `HOSPITALIZATION_SETTINGS` en `0`.
- `SCHEMA_USER_TABLES=46 EXPECTED=46`, sin tablas faltantes ni inesperadas.
- Mismos conteos de modulos, permisos validos y catalogos que el precheck
  (`BASELINE_COUNTS`).
- 0 objetos invalidos; constraints, indices y las 57 FKs `ENABLED/VALIDATED`.

### 5.6 Despues del reset

- Arrancar Backend y ChatBot.
- Iniciar sesion con el SuperAdmin (todas las sesiones previas quedan
  invalidadas porque `USER_TOKENS` se vacia).
- Crear desde la aplicacion los catalogos comerciales (servicios,
  medicamentos, procedimientos, insumos), el staff y la tarifa de
  hospitalizacion.

## 6. Backup y restore con Data Pump

Lo ejecuta un DBA u operador autorizado. Placeholders solamente.

### 6.1 Export (obligatorio)

```bash
expdp <USUARIO_DP>@<ALIAS_TNS> \
  SCHEMAS=<ESQUEMA_ESPERADO> \
  DIRECTORY=<DIRECTORIO_DATAPUMP> \
  DUMPFILE=<PREFIJO>_pre_reset_%U.dmp \
  LOGFILE=<PREFIJO>_pre_reset_exp.log \
  FLASHBACK_TIME=SYSTIMESTAMP
```

- Ejecutar con Backend y ChatBot ya detenidos.
- Revisar que el log termine sin errores y anotar el numero de filas por tabla.
- Copiar el dump y el log a un almacenamiento seguro, con permisos
  restringidos, y registrar su checksum.

### 6.2 Prueba del dump (obligatoria antes de Produccion)

- Generar el DDL sin aplicar nada, para confirmar que el dump es legible:
  `impdp <USUARIO_DP>@<ALIAS_TNS> DIRECTORY=<DIRECTORIO_DATAPUMP> DUMPFILE=<PREFIJO>_pre_reset_%U.dmp SQLFILE=<PREFIJO>_ddl.sql`
- Importarlo en un esquema desechable de la instancia temporal con
  `REMAP_SCHEMA=<ESQUEMA_ESPERADO>:<ESQUEMA_PRUEBA>` y comparar los conteos
  del precheck en ambos.

### 6.3 Restore (rollback despues de la confirmacion)

Antes de la confirmacion final el rollback es automatico. Despues, la unica
via soportada es restaurar el export:

1. Detener Backend y ChatBot.
2. El DBA deja el esquema en un estado vacio acordado (procedimiento del DBA,
   fuera de estos scripts) e importa el dump completo:
   `impdp <USUARIO_DP>@<ALIAS_TNS> SCHEMAS=<ESQUEMA_ESPERADO> DIRECTORY=<DIRECTORIO_DATAPUMP> DUMPFILE=<PREFIJO>_pre_reset_%U.dmp LOGFILE=<PREFIJO>_restore.log`
3. Ejecutar el precheck: debe volver a mostrar los conteos anotados antes del
   reset.
4. Recompilar o validar objetos invalidos si el log lo indica.

El export de esquema incluye la tabla manual historica
`CLIENTS_PHONE_FIX_20260925`. Despues del reset, **la unica forma de revertir
su contenido es restaurar este Data Pump**; no existe otra copia.

No se recomienda restaurar solo datos (`CONTENT=DATA_ONLY`) sobre el esquema
reseteado: las filas conservadas (SuperAdmin, roles, permisos, catalogos)
provocan conflictos de clave y el orden de carga de Data Pump no respeta las
FKs. Flashback Query puede ayudar en una emergencia si la retencion de undo lo
permite, pero no sustituye al export.

## 7. Validacion en instancia Oracle temporal externa (antes de Produccion)

1. Restaurar en una instancia Oracle temporal externa, aislada de
   Produccion, una copia reciente del esquema (el dump de Produccion o una
   copia equivalente), con su propio esquema y PDB.
2. Ejecutar el precheck y guardar el spool.
3. Hacer un export Data Pump y probarlo (seccion 6.2).
4. Ejecutar el reset y el postcheck. Confirmar el criterio final
   (seccion 2.2.1): `USERS_TOTAL=1`, `ROLES_TOTAL=5`, `ROLES_UNEXPECTED=0`,
   `CLIENT_ROLE=0`, `ROLES_NAMED_CLIENTE=0`, `CLIENT_USERS=0`,
   `CLIENT_PERMISSIONS=0` y `ROLE_PERMISSIONS_TOTAL` igual al conteo de
   permisos de los cinco roles aprobados.
5. Ejecutar el reset por segunda vez: debe imprimir `ALREADY_RESET` y salir
   con `0`.
6. Casos negativos, cada uno debe abortar sin cambios y con salida distinta de
   cero:
   - frase de confirmacion incorrecta;
   - PDB o esquema incorrectos;
   - un segundo SuperAdmin activo;
   - una fila con `CREATED_AT` futuro en una tabla operativa (simula
     escritura posterior al corte);
   - una fila en `HOSPITALIZATION_SETTINGS`;
   - un rol adicional o un rol con nombre Cliente bajo otro ID;
   - `CLIENTS_PHONE_FIX_20260925` ausente (en una copia desechable): el
     precheck debe dar `AUX_TABLE_MISSING` / `NOT_READY`, el reset debe
     fallar con `ORA-00942` antes de cualquier `DELETE` (no con `-20023`) y
     el postcheck debe fallar con `AUX_TABLE_MISSING`. Confirmar despues con
     conteos que ninguna otra tabla cambio;
   - una fila de `CLIENTS_PHONE_FIX_20260925` con `FIXED_AT` nulo o posterior
     al corte (en una copia desechable);
   - `FIXED_AT` con un tipo distinto de `TIMESTAMP WITH TIME ZONE` (en una
     copia desechable): precheck `AUX_FIXED_AT_TYPE_UNSUPPORTED` y reset
     abortado con `-20024`.
7. Verificar que `DBMS_OUTPUT` aparece en el spool cuando el script aborta y
   anotar el codigo de salida real de cada caso.
8. Restaurar el esquema desde el dump (seccion 6.3) y confirmar con el
   precheck que los conteos coinciden con los originales. Esto valida el
   rollback.
9. Arrancar Backend y ChatBot contra el esquema reseteado e iniciar sesion
   con el SuperAdmin. En el frontend se ven cuatro plantillas de rol
   (SuperAdmin queda oculto como rol tecnico); en Oracle deben seguir
   existiendo cinco roles.

Solo con todos estos pasos documentados, el PR aprobado y la aprobacion
explicita de la lider se puede planificar la ventana de Produccion.

## 8. Que no hace

- No ejecuta DDL: no crea, altera ni elimina tablas, constraints, indices ni
  secuencias, y no desactiva constraints.
- No vuelve a sembrar datos ni ejecuta migraciones.
- No configura la tarifa de hospitalizacion (`HOSPITALIZATION_SETTINGS` queda
  en `0`).
- No limpia Redis ni Qdrant.
- No toca archivos subidos (fotos de mascotas u otros) ni almacenamiento fuera
  de Oracle.
- No bloquea tablas (`LOCK TABLE`): depende de que los servicios esten
  detenidos.
- No se conecta a ningun entorno por si mismo: solo se ejecuta cuando un
  operador lo lanza en SQL*Plus.

## 9. Limpieza fuera de Oracle (separada y con aprobacion propia)

Cada paso requiere aprobacion independiente de la lider. Primero se
inspecciona (solo conteos) y despues se decide.

### 9.1 Telegram

- Antes del reset: con el bot detenido, consultar el estado del webhook o de
  la cola (`getWebhookInfo`, campo `pending_update_count`) sin imprimir el
  token del bot.
- Si hay updates pendientes de datos de prueba y se aprueba descartarlos,
  usar `drop_pending_updates=true` al reconfigurar el webhook.

### 9.2 Redis (checkpoints y estado del ChatBot)

- Inspeccionar con `SCAN` y patrones concretos, contando claves por prefijo.
  **Nunca** usar `KEYS *` ni `FLUSHALL`/`FLUSHDB` sin aprobacion explicita.
- Los checkpoints tienen TTL (7 dias segun la documentacion del ChatBot);
  esperar a que expiren puede ser suficiente.
- Si se aprueba, borrar solo los prefijos acordados.

### 9.3 Qdrant

- Listar colecciones y conteos de puntos, sin volcar payloads.
- Distinguir colecciones de conocimiento (se conservan) de cualquier memoria
  por conversacion o cliente (candidata a limpieza).
- Borrar solo las colecciones o puntos aprobados.

## 10. Scripts que NO se deben usar

- `database/seeds/limpieza_total.sql`
- `database/seeds/extra/apply_all.sql`

No tienen guardas de entorno ni validaciones. Ademas, `apply_all.sql`
referencia la tabla eliminada `AI_RUNS_STATUSES` y siembra datos que este reset
elimina.

## 11. Limitaciones y decisiones pendientes

1. **Topes de volumen**: son propuestas y requieren aprobacion de la lider
   antes de Produccion.
2. **Rol Cliente**: se elimina por su ID fijo `77777777-...`, y antes se
   valida que ese ID tenga `NAME = 'Cliente'`; si tiene otro nombre, o si
   existe un rol llamado Cliente bajo otro ID, el reset aborta. El postcheck
   exige 0 roles Cliente por ID y por nombre.
   **Criterio aprobado**: en Oracle quedan exactamente cinco roles
   (SuperAdmin, Administrador, Veterinario, Recepcionista, Auxiliar), aunque
   el frontend muestre cuatro plantillas.
3. **Estados de escalamiento**: se exigen los 5 IDs fijos `85000000-...`.
   Hay que confirmar que `PendingEscalationStatusId` de la configuracion del
   ChatBot/Backend apunta a uno de ellos.
4. **Catalogos sembrados por nombre** (`STATUS_APPOINTMENTS`, `SPECIES`,
   `RACES`, `SPECIALTIES`, `TYPE_SERVICES`): sus IDs no estan garantizados;
   se validan conteos (y, en el reset, una huella de sus PKs). No hay un
   conteo absoluto esperado de modulos ni catalogos: se exige que no esten
   vacios y que no cambien.
5. **Tablas u objetos legacy**: cualquier tabla o FK fuera del inventario del
   modelo actual (por ejemplo, restos de migraciones antiguas) hace abortar.
   Si aparece, decidir con la lider antes de tocar nada.
6. **USER_TOKENS**: se vacia por completo, incluidas las sesiones del
   SuperAdmin; tendra que iniciar sesion de nuevo.
7. **HOSPITALIZATION_SETTINGS**: debe estar en `0`. Si Produccion ya tiene una
   tarifa configurada, el reset aborta; conservarla o no es una decision
   pendiente.
8. **Concurrencia**: no hay `LOCK TABLE`; el corte UTC detecta escrituras
   previas a la captura, no las posteriores. Detener los servicios es
   obligatorio.
9. **DBMS_OUTPUT al abortar**: se debe verificar en la instancia temporal que las lineas
   aparecen en el spool; el mensaje del error incluye las razones como
   respaldo.
10. **SQL dinamico**: `EXECUTE IMMEDIATE` se usa solo para `SELECT COUNT(*)`
    y huellas sobre nombres de una lista fija, con `DBMS_ASSERT.ENQUOTE_NAME`.
11. **Postcheck independiente**: al ser otra sesion, compara conteos
    (`BASELINE_COUNTS`), no huellas. La comparacion por huella de catalogos,
    modulos y permisos se hace dentro del reset antes de confirmar.
12. **Rollback despues de la confirmacion**: el procedimiento concreto del DBA
    para dejar el esquema listo para la importacion debe definirse y probarse
    en la instancia temporal.
13. **CLIENTS_PHONE_FIX_20260925 ausente**: se mantiene obligatoria y con
    `DELETE` estatico. Precheck: `AUX_TABLE_MISSING` / `NOT_READY`. Reset:
    Oracle puede fallar con `ORA-00942` durante el parseo o la compilacion,
    antes de cualquier `DELETE`; es un fallo seguro y **no** equivale al
    codigo de guarda `-20023`. Postcheck: `AUX_TABLE_MISSING` como fallo.
    **Pendiente**: validar este comportamiento en la instancia temporal
    restaurada desde el Data Pump antes de considerar aprobado el runbook.
14. **Tipo de `FIXED_AT`**: la auditoria Oracle confirmo
    `TIMESTAMP(6) WITH TIME ZONE`. Precheck y reset exigen
    `TIMESTAMP WITH TIME ZONE` y rechazan `DATE`, `TIMESTAMP` sin zona y
    `TIMESTAMP WITH LOCAL TIME ZONE`, para que `SYS_EXTRACT_UTC(FIXED_AT)`
    no dependa de la zona horaria de la sesion. Confirmar
    `AUX_FIXED_AT_TYPE` en el precheck de la instancia temporal.
15. **Tope de la tabla manual historica**: el valor inicial `6` debe
    confirmarse o recapturarse con el conteo real durante la ventana.
