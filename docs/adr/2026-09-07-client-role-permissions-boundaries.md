# ADR: Definici├│n de l├¡mites y desasignaci├│n de permisos de m├│dulos de plataforma web para el rol Cliente

- **Estado**: Aceptada
- **Fecha**: 2026-09-07
- **Decisores**: Equipo de desarrollo

## Contexto

El rol `Cliente` (representado por la persona/usuario due├▒o de mascota con ID fijo `77777777-7777-7777-7777-777777777777`) interact├║a con el sistema veterinario a trav├®s del chatbot conversacional (ej. Telegram) y servicios de autoservicio (autenticaci├│n por OTP de identidad).

Anteriormente, el script de sembrado inicial `database/seeds/role_permissions_seed.sql` asignaba permisos de lectura (`CAN_VIEW = 1`) para el rol `Cliente` en los m├│dulos de plataforma web `Clientes`, `Mascotas`, `Citas` e `Historiales Cl├¡nicos`.

Esta configuraci├│n induc├¡a a error al sugerir que el cliente cuenta con acceso a un portal/men├║ web administrativo y expone permisos en la matriz `/auth/permissions`, cuando la arquitectura del producto define que el cliente no opera la interfaz administrativa web de la veterinaria.

## Opciones Consideradas

1. **Opci├│n A (Seleccionada)**: Eliminar todas las filas de permisos de m├│dulos de plataforma web para el rol `Cliente` en `ROLE_PERMISSIONS`. El rol queda registrado en `ROLES`, pero sin asociarle permisos a nivel de men├║ web.
2. **Opci├│n B**: Mantener una fila m├¡nima con comentario expl├¡cito en `ROLE_PERMISSIONS` por si un servicio interno heredado lee de esta tabla para el cliente.

## Decisi├│n

Se adopta la **Opci├│n A**. 

1. **Sin filas en `ROLE_PERMISSIONS` para Cliente**: el seed **no inserta** m├│dulos de plataforma para ese rol y, al reejecutar, hace `DELETE FROM ROLE_PERMISSIONS WHERE ROLE_ID = 77777777-ÔÇª` para retirar filas residuales de semillas anteriores (`ensure_permission` solo hace INSERT en NOT MATCHED).
2. **Autorizaci├│n basada en Pol├¡ticas de Dominio y Recursos**: La autorizaci├│n del `Cliente` se realiza mediante pol├¡ticas de API (`ClientOnly` en retiro Etapa 5), ownership de recursos (`user_id` / `client_id`) y verificaci├│n por OTPs, no mediante la matriz din├ímica de permisos de m├│dulos de plataforma.
3. **Claridad conceptual**: Se deja expl├¡cito mediante comentarios en los scripts de seed que el rol `Cliente` es de uso exclusivo para chatbot/Telegram y autoservicio (no web).

## Consecuencias

- Previene la simulaci├│n err├│nea de men├║s o accesos web para usuarios con rol `Cliente` en el token JWT o endpoint `/auth/permissions`.
- Los permisos asignados al Staff (`Administrador`, `Veterinario`, `Recepcionista`, `Auxiliar`) se mantienen completamente intactos.
- La semilla es idempotente y segura para reejecuciones.
