# Permisos efectivos en JWT

## Objetivo

Evitar las consultas a Oracle que actualmente se ejecutan para autorizar cada endpoint protegido con `RequirePermission`, manteniendo los permisos persistidos en base de datos como fuente de verdad.

## Decisión

El backend calculará los permisos efectivos durante el inicio de sesión y la renovación del token. Cada permiso concedido se incluirá en el access token dentro de una claim `permissions` con valores en el formato que ya usa la autorización dinámica:

```json
{
  "permissions": [
    "perm:Usuarios:View",
    "perm:Mascotas:View",
    "perm:Mascotas:Create",
    "perm:Citas:Edit"
  ]
}
```

El access token tendrá una vigencia predeterminada de 15 minutos. Los cambios de permisos se persistirán inmediatamente en Oracle y aparecerán en el siguiente login o refresh. Una sesión existente podrá conservar permisos anteriores durante un máximo de 15 minutos.

## Fuente de permisos

Para un usuario común, el conjunto emitido será la unión de permisos concedidos por su rol y permisos individuales:

```text
permiso efectivo = permiso del rol OR permiso individual
```

Solo se incluirán acciones cuyo resultado sea `true`. El modelo seguirá siendo aditivo: un permiso individual podrá conceder una acción, pero no revocar una acción concedida por el rol.

El rol SuperAdmin continuará identificándose mediante su `role_id` canónico y conservará el bypass total. Sus tokens no necesitan enumerar todos los permisos.

## Flujo

1. El usuario inicia sesión o presenta un refresh token válido.
2. El backend reconstruye la identidad actual desde Oracle.
3. Obtiene los módulos y calcula los permisos efectivos del rol y del usuario.
4. Convierte cada acción concedida a `perm:{Módulo}:{Acción}`.
5. Emite el JWT firmado mediante RS256 con la claim `permissions`.
6. En cada petición, `PermissionAuthorizationHandler` valida localmente la claim requerida, sin consultar Oracle.

## Compatibilidad del endpoint de permisos

`GET /api/auth/permissions` se conservará para el frontend. Durante una sesión devolverá la matriz reconstruida a partir de las claims del JWT, garantizando que la interfaz represente la misma autorización que aplica el backend para ese token.

Para SuperAdmin seguirá devolviendo todas las acciones en `true` para todos los módulos conocidos.

## Seguridad y consistencia

- El JWT continuará validando firma RS256, emisor, audiencia y expiración.
- `Jwt__AccessTokenMinutes` tendrá el valor recomendado de `15` en la configuración de ejemplo.
- Los refresh tokens conservarán su duración y rotación actuales.
- Cambiar permisos en Oracle no revocará un access token ya emitido; la ventana máxima aceptada será de 15 minutos.
- Cambiar el rol de un usuario tendrá la misma ventana máxima y se reflejará al renovar el token.
- No se incluirán permisos denegados ni información sensible.
- La administración de roles y permisos seguirá protegida exclusivamente para SuperAdmin.

## Tamaño del token

Se incluirán solamente permisos concedidos. Antes de emitir el token se eliminarán duplicados y se usará un orden determinista. Si el catálogo creciera hasta acercarse a los límites habituales de cabeceras HTTP, se deberá migrar a permisos compactos o a una caché distribuida; no se introduce esa complejidad en esta fase.

## Tratamiento de errores

- Si no se puede reconstruir la identidad durante login o refresh, no se emite el token.
- Si falla la lectura de permisos, el proceso falla de forma cerrada y no emite un token incompleto.
- Si una petición no contiene la claim requerida, se devuelve `403 Forbidden`.
- Un JWT ausente, inválido o expirado continúa produciendo `401 Unauthorized`.

## Verificación

Las pruebas deben cubrir como mínimo:

- Emisión de permisos de rol.
- Unión de permisos de rol y usuario.
- Exclusión de acciones denegadas y duplicados.
- Renovación con permisos actualizados.
- Autorización local sin consultas de permisos a Oracle.
- Respuesta `403` cuando falta una claim.
- Bypass de SuperAdmin.
- Reconstrucción de `GET /api/auth/permissions` desde el token.
- Vigencia predeterminada de 15 minutos.

Se ejecutará una selección reducida de pruebas de seguridad y autenticación, de acuerdo con la preferencia del proyecto de evitar suites completas innecesarias durante cambios acotados.
