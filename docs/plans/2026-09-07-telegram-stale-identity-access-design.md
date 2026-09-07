# Recuperación de identidad obsoleta en Telegram

## Problema

Cuando un chat conserva una fila activa en `TELEGRAM_USER_LINKS`, pero la persona vinculada ya no
resuelve a un cliente, usuario y cuenta activos, `TelegramIdentityAccessService` responde que el
perfil no está disponible. El flujo termina antes de solicitar cédula y no permite recuperar el
acceso ni registrar un cliente nuevo.

## Decisión

Una vinculación que no resuelve a una identidad de cliente activa se considera obsoleta. El
backend debe revocarla, conservar su trazabilidad y continuar la misma solicitud privada como un
acceso sin identidad conocida:

1. Capturar de forma protegida el mensaje privado pendiente.
2. Revocar la vinculación obsoleta mediante `TelegramUserLink.Revoke`.
3. Persistir una `TelegramIdentitySession` en estado `AwaitingIdentification`.
4. Solicitar la cédula al usuario.
5. Si la cédula corresponde a un cliente activo, enviar el OTP al correo registrado.
6. Si la cédula no existe, recopilar confirmación, nombre completo y correo; enviar OTP y crear el
   usuario, la cuenta y el perfil de cliente dentro de la transacción existente.
7. Al verificar el OTP, crear o reactivar el enlace y reanudar automáticamente el mensaje original.

## Compatibilidad

- El acceso con una vinculación válida conserva el envío directo de OTP.
- El acceso sin vinculación conserva el flujo actual por cédula.
- `/vincular` y `/registrar` no forman parte del flujo nuevo ni se muestran al usuario.
- Los componentes heredados de vinculación permanecen sin cambios para evitar una eliminación
  incompatible en esta corrección.
- No cambia ningún endpoint, contrato HTTP, tabla ni migración.

## Capas afectadas

- Domain: sin cambios; se reutiliza `TelegramUserLink.Revoke`.
- Application: cambia `TelegramIdentityAccessService.BeginPrivateAccessAsync`.
- Infrastructure: sin cambios; se reutilizan los repositorios existentes.
- Api: sin cambios; Telegram continúa entrando por el webhook actual.

## Errores y consistencia

La revocación del enlace, la creación de la sesión y el guardado deben completarse antes de
responder que se requiere la cédula. Los errores de persistencia conservan el mecanismo actual de
reintentos del procesamiento de Telegram. No se registra en logs la cédula, el OTP, el mensaje ni
el correo.

## Verificación

Una prueba de Application reproducirá una vinculación activa cuyo `PersonId` no resuelve a una
identidad de cliente. Debe comprobar que el enlace queda revocado, se crea la sesión de
identificación, se guarda el mensaje pendiente y la respuesta solicita la cédula. También se
ejecutarán las pruebas existentes de `TelegramIdentityAccessService` y una compilación sin restore.
