# Seeds de producción

Estos scripts crean únicamente catálogos iniciales. Son idempotentes y no incluyen usuarios, contraseñas, clientes, mascotas, citas ni historias clínicas de prueba.

## Orden automático

Desde la raíz del backend, con las migraciones ya aplicadas:

```powershell
$env:NLS_LANG = "SPANISH_SPAIN.AL32UTF8"
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1521/FREEPDB1' '@database\seeds\apply_all.sql'
```

`apply_all.sql` ejecuta, en orden:

1. `roles_seed.sql`
2. `modules_seed.sql`
3. `role_permissions_seed.sql`
4. `status_appointments_seed.sql`
5. `chat_conversation_catalogs_seed.sql`
6. `chat_runtime_catalogs_seed.sql`
7. `veterinary_catalogs_seed.sql`
8. `verify_seeds.sql`

El ejecutor nunca llama `cleanup_seeds.sql` ni scripts de `database/test_seeds`.

## Valores mínimos esperados

| Catálogo | Cantidad canónica mínima |
|---|---:|
| Roles | 6 |
| Modules | 20 |
| Role permissions | 44 |
| Appointment statuses | 6 |
| Conversation statuses | 4 |
| Sender types | 4 |
| Message types | 5 |
| Priorities | 4 |
| Escalation statuses | 5 |
| AI run statuses | 5 |
| Type services | 5 |
| Species | 3 |
| Specialties | 5 |

Las cantidades reales pueden ser mayores si la clínica agregó valores propios. `verify_seeds.sql` permite inspeccionar el resultado.

## Primer SuperAdmin

El seed agrega el rol protegido, pero no crea una cuenta personal. Primero debe existir una cuenta interna activa con credencial. Después, un administrador de Oracle puede promoverla explícitamente:

```powershell
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1521/FREEPDB1' `
  '@database\admin\promote_superadmin.sql' `
  'correo-de-la-cuenta@dominio.com'
```

La operación cambia el rol y elimina los refresh tokens de esa cuenta. La persona debe iniciar sesión nuevamente.

## Seguridad

- No agregue correos, contraseñas ni hashes a estos scripts.
- No ejecute `cleanup_seeds.sql` como parte de una instalación normal.
- Confirme siempre el servicio y esquema Oracle antes de ejecutar un script.
- Ejecute el aprovisionamiento con una cuenta de base de datos autorizada para actualizar las tablas indicadas.
