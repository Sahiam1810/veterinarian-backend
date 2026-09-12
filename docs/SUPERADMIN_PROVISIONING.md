# Aprovisionamiento seguro de SuperAdmin

El SuperAdmin es una identidad persistida en Oracle. No se configura mediante variables de entorno, no tiene un inicio de sesión especial y no se crea con credenciales incluidas en seeds.

## Requisitos

1. Aplicar las migraciones del backend.
2. Aplicar los seeds de producción de `database/seeds/apply_all.sql`.
3. Tener una cuenta interna activa con `USERS`, `USER_ACCOUNTS` y `USER_CREDENTIALS` válidos.
4. Ejecutar el proceso con un usuario Oracle autorizado para actualizar esas tablas.

## Promover una cuenta existente

Desde la raíz del backend:

```powershell
$env:NLS_LANG = "SPANISH_SPAIN.AL32UTF8"
& 'C:\ruta\a\sqlplus.exe' 'VET_APP@//localhost:1521/FREEPDB1' `
  '@database\admin\promote_superadmin.sql' `
  'correo-de-la-cuenta@dominio.com'
```

El script exige que exista exactamente una cuenta activa con credencial, asigna el rol canónico `SuperAdmin` y elimina sus refresh tokens anteriores. No crea usuarios, contraseñas ni datos personales.

## Verificación

1. Cierre cualquier sesión anterior e inicie sesión nuevamente.
2. Compruebe que `GET /api/auth/me` devuelve `role: SuperAdmin`.
3. Compruebe que `GET /api/auth/permissions` devuelve los permisos completos.
4. Compruebe que una cuenta con rol `Administrador` recibe `403` al intentar una operación protegida por `SuperAdminOnly`.

Los JWT antiguos que solo tengan `super_admin=true` no son válidos como autoridad de SuperAdmin. La autorización requiere el `role_id` canónico emitido desde la identidad persistida.

## Reglas de seguridad

- No agregue correos, contraseñas, hashes ni claves privadas a los scripts o al repositorio.
- No restaure las variables `SuperAdmin__*`; ya no forman parte de la configuración.
- Mantenga las claves RSA JWT únicamente en las variables `Jwt__*` correspondientes.
- Confirme el servicio y el esquema Oracle antes de ejecutar el script.
