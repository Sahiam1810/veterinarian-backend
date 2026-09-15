# ADR: Definición de límites y desasignación de permisos de módulos de plataforma web para el rol Cliente

- **Estado**: Aceptada
- **Fecha**: 2026-09-07
- **Decisores**: Equipo de desarrollo

## Contexto

El rol `Cliente` (representado por la persona/usuario dueño de mascota con ID fijo `77777777-7777-7777-7777-777777777777`) interactúa con el sistema veterinario a través del chatbot conversacional (ej. Telegram) y servicios de autoservicio (autenticación por OTP de identidad).

Anteriormente, el script de sembrado inicial `database/seeds/role_permissions_seed.sql` asignaba permisos de lectura (`CAN_VIEW = 1`) para el rol `Cliente` en los módulos de plataforma web `Clientes`, `Mascotas`, `Citas` e `Historiales Clínicos`.

Esta configuración inducía a error al sugerir que el cliente cuenta con acceso a un portal/menú web administrativo y expone permisos en la matriz `/auth/permissions`, cuando la arquitectura del producto define que el cliente no opera la interfaz administrativa web de la veterinaria.

## Opciones Consideradas

1. **Opción A (Seleccionada)**: Eliminar todas las filas de permisos de módulos de plataforma web para el rol `Cliente` en `ROLE_PERMISSIONS`. El rol queda registrado en `ROLES`, pero sin asociarle permisos a nivel de menú web.
2. **Opción B**: Mantener una fila mínima con comentario explícito en `ROLE_PERMISSIONS` por si un servicio interno heredado lee de esta tabla para el cliente.

## Decisión

Se adopta la **Opción A**.

1. **Sin filas en `ROLE_PERMISSIONS` para Cliente**: el seed **no inserta** módulos de plataforma para ese rol y, al reejecutar, hace `DELETE FROM ROLE_PERMISSIONS WHERE ROLE_ID = 77777777-…` para retirar filas residuales de semillas anteriores (`ensure_permission` solo hace INSERT en NOT MATCHED).
2. **Autorización basada en políticas de dominio y recursos**: La autorización del `Cliente` se realiza mediante ownership de recursos (`user_id` / `client_id`) y verificación por OTPs (chatbot/autoservicio), no mediante la matriz dinámica de permisos de módulos de plataforma. En Etapa 5 se retiran las rutas JWT del portal Cliente (`410 Gone` / `ClientPortal.Gone`) y se elimina la política `ClientOnly`.
3. **Claridad conceptual**: Se deja explícito mediante comentarios en los scripts de seed que el rol `Cliente` es de uso exclusivo para chatbot/Telegram y autoservicio (no web).

## Consecuencias

- Previene la simulación errónea de menús o accesos web para usuarios con rol `Cliente` en el token JWT o endpoint `/auth/permissions`.
- Los permisos asignados al Staff (`Administrador`, `Veterinario`, `Recepcionista`, `Auxiliar`) se mantienen completamente intactos.
- La semilla es idempotente y segura para reejecuciones.
- Las rutas legacy del portal Cliente JWT responden `410 Gone` con código estable `ClientPortal.Gone`; OTP anónimo (request-code/confirm-code) permanece operativo.
