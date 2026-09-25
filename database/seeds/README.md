# Seeds del Sistema

Scripts para inicializar catálogos base, permisos, usuarios del staff y cuentas de acceso en Oracle.

## Ejecución

Requisito: las migraciones ya aplicadas (`dotnet ef database update`). Los scripts se pueden correr desde **SQL Developer** (abrir una hoja de trabajo, pegar las líneas `@...` y pulsar **F5**) o con SQL*Plus.

### Desarrollo local (recomendado): un solo script

`insert_all_seeds.sql` carga todo lo necesario para trabajar: roles, módulos y permisos, catálogos (incluidos los del chat que usa el bot), personal con cuenta y contraseña, clientes de demo, mascotas, citas y una historia clínica. En SQL Developer:

```sql
SET DEFINE OFF;
@C:\<ruta>\veterinarian-backend\database\seeds\insert_all_seeds.sql
@C:\<ruta>\veterinarian-backend\database\seeds\verify_seeds.sql
```

Para empezar de cero: `dotnet ef database drop` + `dotnet ef database update` (o `limpieza_total.sql`, que vacía los datos sin borrar las tablas).

### Instalación limpia sin datos de prueba: `extra/`

`extra/apply_all.sql` carga solo catálogos, roles y permisos: **no** crea usuarios, clientes ni datos de demo.

```powershell
$env:NLS_LANG = "SPANISH_SPAIN.AL32UTF8"
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1522/FREEPDB1' '@database\seeds\extra\apply_all.sql'
```

Orden de ejecución: `roles_seed.sql`, `modules_seed.sql`, `role_permissions_seed.sql`, `status_appointments_seed.sql`, `chat_conversation_catalogs_seed.sql`, `chat_runtime_catalogs_seed.sql`, `veterinary_catalogs_seed.sql`, `diagnostics_seed.sql`. Si ejecutas los archivos uno por uno (por ejemplo desde SQL Developer), respeta ese orden. `apply_all.sql` termina llamando a `verify_seeds.sql`, que está en la carpeta superior: si esa última línea falla por no encontrar el archivo, ejecuta `verify_seeds.sql` aparte.

> **No mezcles ambos en la misma base.** Los catálogos veterinarios de `insert_all_seeds.sql` (Canino, Felino…) son distintos de los de `extra/` (Perro, Gato…) y quedarían duplicados. Si ya cargaste uno y quieres el otro, resetea la base.

### Datos de demo para pruebas: `demo_data_pruebas.sql`

Carga, **después** de `insert_all_seeds.sql` (o de `extra/apply_all.sql`), datos para probar el flujo completo: unos 45 servicios con precio, 53 medicamentos y 50 procedimientos (radiografías, laboratorios, cirugías…) para las órdenes médicas, 31 insumos con stock, los permisos de Órdenes Médicas / Hospitalización / Insumos por rol, horario Lunes-Viernes 7:00-17:00 para cada veterinario y 12 dueños con 18 mascotas. Es idempotente: cada fila se busca por nombre o cédula y solo se inserta si no existe.

```sql
SET DEFINE OFF;
@C:\<ruta>eterinarian-backend\database\seeds\demo_data_pruebas.sql
```

Después de correrlo, cierra sesión y vuelve a entrar con cada rol: los permisos viajan en el token.

---

## Cuentas y Credenciales de Prueba

Todos los usuarios de Staff quedan activos (`USERS.IS_ACTIVE = 1`) y con contraseña lista para iniciar sesión en la web (login por correo, sin usuario/username separado).

* **Contraseña general (Staff):** `Password123!`

| Rol | Correo / Login | Acceso Web |
|---|---|:---:|
| **SuperAdmin** | `superadmin@veterinaria.com` | ✅ Sí |
| **Administrador** | `admin@veterinaria.com` | ✅ Sí |
| **Veterinario** | `veterinario@veterinaria.com` | ✅ Sí |
| **Recepcionista** | `recepcionista@veterinaria.com` | ✅ Sí |
| **Auxiliar** | `auxiliar@veterinaria.com` | ✅ Sí |

> **Nota:** Los dueños de mascota (clientes) no se modelan como usuarios de plataforma: viven en `CLIENTS` y se identifican vía chatbot/Telegram.

---

## Valores mínimos esperados

Valores de la instalación limpia (`extra/`). El seed de desarrollo (`insert_all_seeds.sql`) trae menos especies, razas y especialidades, pero los mismos catálogos del chat.

| Catálogo | Cantidad mínima |
|---|---:|
| Roles | 5 |
| Modules | 23 |
| Role permissions | 44 |
| Appointment statuses | 6 |
| Conversation statuses | 4 |
| Sender types | 4 |
| Message types | 5 |
| Priorities | 4 |
| Escalation statuses | 5 |
| AI run statuses | 5 |
| Type services | 5 |
| Species | 5 |
| Races | 28 |
| Specialties | 5 |
| Diagnostics | 12 |

Las cantidades reales pueden ser mayores si la clínica agregó valores propios. `verify_seeds.sql` permite inspeccionar el resultado.

## Reparar bases de datos locales desactualizadas

`role_permissions_seed.sql` solo inserta filas faltantes: si su base local ya tenía una
fila de `ROLE_PERMISSIONS` creada por una versión anterior del seed (por ejemplo, antes
de que existiera el módulo `Plataforma`), volver a correr `extra/apply_all.sql` no corrige los
flags de esa fila. Si el rol Veterinario, Recepcionista o Auxiliar recibe 403 al usar el
panel aunque los permisos "se vean bien", probablemente su base quedó en ese estado.

Ejecute primero `extra/apply_all.sql` (crea el módulo `Plataforma` y cualquier módulo/rol
faltante) y luego:

```powershell
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1521/FREEPDB1' '@database\seeds\extra\role_permissions_repair_2026-09-10.sql'
```

Este script sí actualiza filas existentes para dejarlas iguales a `role_permissions_seed.sql`. Es idempotente.

## Primer SuperAdmin

El seed agrega el rol protegido, pero no crea una cuenta personal. Primero debe existir una cuenta interna activa con credencial. Después, un administrador de Oracle puede promoverla explícitamente:

```powershell
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1522/FREEPDB1' `
  '@database\admin\promote_superadmin.sql' `
  'correo-de-la-cuenta@dominio.com'
```

La operación cambia el rol y elimina los refresh tokens de esa cuenta. La persona debe iniciar sesión nuevamente.


## Notas rápidas

- Las inserciones son idempotentes (`MERGE INTO`), no duplican datos si se ejecutan varias veces.
- Si se requiere vaciar completamente la base de datos para empezar de cero, ejecutar primero `limpieza_total.sql` (o resetear la base con `dotnet ef database drop` + `update`).