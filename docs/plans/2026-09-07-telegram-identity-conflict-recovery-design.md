# Recuperación del flujo de identidad de Telegram

## Problema

Cuando una solicitud privada inicia un registro y el correo verificado por OTP ya pertenece
a una cuenta, `StageRegistrationAsync` informa un conflicto. El servicio cancela la sesión,
borra la solicitud pendiente y el siguiente mensaje vuelve al chat general. La respuesta
indica el problema, pero no mantiene un camino ejecutable para resolverlo.

## Decisión

Después de validar el OTP se distinguirán tres situaciones:

1. Correo nuevo: crear `User`, `UserAccount` y `Client`, vincular Telegram y reanudar la
   solicitud privada original.
2. Cuenta activa sin perfil `Client`: crear únicamente el perfil `Client` con la cédula ya
   confirmada como disponible, vincular Telegram y reanudar la solicitud.
3. Cuenta activa con perfil `Client`:
   - si la cédula coincide, vincular y reanudar;
   - si no coincide, conservar la solicitud pendiente, limpiar los datos temporales de
     registro y regresar a `AwaitingIdentification` para solicitar la cédula registrada.

Las cuentas inactivas y los conflictos donde la cédula pertenece a otra persona no se
vincularán automáticamente. El usuario recibirá una instrucción concreta para corregir la
cédula o cancelar.

## Arquitectura

- **Domain:** `TelegramIdentitySession` incorporará una transición explícita de recuperación
  hacia `AwaitingIdentification` que conserva el mensaje y el `PendingInboundUpdateId`, pero
  elimina nombre, correo, OTP y persona temporal.
- **Application:** el flujo consultará el estado del correo antes de enviar el OTP y conservará
  en la sesión la persona existente. Tras validar el código solicitará al gateway resolver o
  crear exclusivamente el perfil de cliente permitido.
- **Infrastructure:** el gateway resolverá la cuenta activa por correo/persona y podrá añadir
  un `Client` a una cuenta activa que todavía no lo tenga. Nunca modificará una cédula ya
  persistida ni moverá un perfil entre personas.
- **Api/esquema:** sin cambios. El webhook, los contratos HTTP y las tablas permanecen iguales.

## Seguridad y consistencia

- El correo existente solo se usa después de que su OTP sea válido.
- Una cédula ya perteneciente a otra persona sigue siendo conflicto.
- Una cuenta con perfil y cédula diferente exige volver a introducir la cédula correcta.
- Toda creación/vinculación continúa dentro de la transacción existente.
- Los datos temporales sensibles se limpian al recuperar la sesión.
- La solicitud privada original se consume únicamente después de verificar y vincular.

## Compatibilidad y pruebas

El cambio es compatible en contratos y esquema, pero modifica el comportamiento del conflicto.
Se cubrirán pruebas enfocadas para cuenta nueva, cuenta activa sin cliente, coincidencia por
cédula, cédula diferente, cuenta inactiva y conservación/reanudación del mensaje pendiente.
No se aplicarán migraciones ni se modificarán endpoints.
