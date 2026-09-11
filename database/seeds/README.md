# Seeds del Sistema

Scripts para inicializar catálogos base, permisos, usuarios del staff y cuentas de acceso en Oracle.

## Ejecución

Desde la raíz del backend con las migraciones ya aplicadas:

```powershell
$env:NLS_LANG = "SPANISH_SPAIN.AL32UTF8"
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1522/FREEPDB1' '@database\seeds\apply_all.sql'
```

Orden de ejecución:
1. `roles_seed.sql`
2. `modules_seed.sql`
3. `veterinary_catalogs_seed.sql` (Especies, Razas, Especialidades, Tipos de Servicio)
4. `diagnostics_seed.sql`
5. `status_appointments_seed.sql`
6. `chat_conversation_catalogs_seed.sql`
7. `chat_runtime_catalogs_seed.sql`
8. `role_permissions_seed.sql`
9. `users_and_accounts_seed.sql`

---

## Cuentas y Credenciales de Prueba

Todos los usuarios de Staff tienen cuenta creada en `USER_ACCOUNTS` (`STATUS = 'Activo'`) y credenciales listas para iniciar sesión en la web.

* **Contraseña general (Staff):** `Password123!`

| Rol | Correo / Login | Usuario | Acceso Web |
|---|---|---|:---:|
| **SuperAdmin** | `superadmin@veterinaria.com` | `superadmin` | ✅ Sí |
| **Administrador** | `admin@veterinaria.com` | `admin` | ✅ Sí |
| **Veterinario** | `veterinario@veterinaria.com` | `veterinario` | ✅ Sí |
| **Recepcionista** | `recepcionista@veterinaria.com` | `recepcionista` | ✅ Sí |
| **Auxiliar** | `auxiliar@veterinaria.com` | `auxiliar` | ✅ Sí |
| **Cliente** | `cliente@veterinaria.com` | — | ❌ No (solo chatbot/OTP) |

> **Nota sobre Clientes:** Por regla de seguridad, los clientes no tienen contraseña ni registro en `USER_ACCOUNTS`.

---

## Valores mínimos esperados

| Catálogo | Cantidad mínima |
|---|---:|
| Roles | 6 |
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
de que existiera el módulo `Plataforma`), volver a correr `apply_all.sql` no corrige los
flags de esa fila. Si el rol Veterinario, Recepcionista o Auxiliar recibe 403 al usar el
panel aunque los permisos "se vean bien", probablemente su base quedó en ese estado.

Ejecute primero `apply_all.sql` (crea el módulo `Plataforma` y cualquier módulo/rol
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
- Si se requiere vaciar completamente la base de datos para empezar de cero, ejecutar primero `cleanup_seeds.sql`.