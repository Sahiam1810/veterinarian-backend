# Diseño de orientación de vinculación en Telegram

## Objetivo

Evitar que el bot agregue `/vincular` a todas las respuestas invitadas, sin
perder la explicación inicial ni la protección de datos y operaciones privadas.
Este incremento prepara la experiencia para el futuro módulo `pet_profile`,
pero no implementa ni registra ningún módulo veterinario.

## Alcance aprobado

- `/start` en un chat no vinculado explica una sola vez los modos invitado y
  vinculado.
- Una pregunta general invitada recibe solamente la respuesta del agente; .NET
  no agrega un sufijo fijo con `/vincular`.
- El prompt invitado conserva la obligación de orientar a `/vincular` cuando la
  solicitud requiere mascotas, citas, vacunas, historia clínica, cuenta u otra
  operación privada.
- `/vincular`, correo, OTP, `/cancelar` y `/desvincular` conservan el flujo
  existente.
- Si el usuario no posee cuenta, se le explica que debe crearla mediante la
  aplicación. No se solicita contraseña, identificación ni datos de registro en
  Telegram.
- El enlace seguro de registro se incorporará cuando exista una URL pública de
  frontend; este incremento no inventa ni expone una ruta de Swagger.

## Decisión arquitectónica

.NET continúa siendo propietario del canal y de los comandos de vinculación.
El agente continúa siendo propietario de la respuesta conversacional general
y de decidir, mediante su política segura, si una solicitud necesita identidad
personal. Se elimina únicamente el sufijo incondicional que .NET agrega después
de cada respuesta invitada.

La respuesta de `/start` permanece estática en .NET y no consume el modelo. El
agente mantiene el rol cerrado `TelegramGuest`, no ejecuta módulos, no reutiliza
respuestas RAG directas y no publica conocimiento global.

## Flujo

```text
Telegram no vinculado
  /start
    -> respuesta estática: puede preguntar en modo invitado
    -> explica /vincular para datos privados
    -> explica que una cuenta nueva se crea en la aplicación

  pregunta general
    -> JWT TelegramGuest
    -> agente general seguro
    -> respuesta sin sufijo automático

  solicitud privada
    -> JWT TelegramGuest
    -> agente general seguro
    -> orientación contextual a /vincular

  /vincular
    -> flujo persistente correo + OTP existente
```

## Manejo de errores y seguridad

- Una respuesta vacía del agente conserva la bienvenida segura existente.
- Un fallo del agente conserva el procesamiento técnico actual; no se convierte
  en una invitación falsa a registrarse.
- Telegram nunca recibe ni recopila contraseñas.
- No se registran mensajes, JWT, correo, OTP, Telegram ID ni UUID invitado.
- `Telegram__GuestModeEnabled=false` conserva el modo estricto anterior.
- El cambio no crea tablas, migraciones ni registros adicionales en Oracle.

## Pruebas

- `/start` invitado explica ambos modos sin llamar al agente.
- Una respuesta general invitada se entrega sin `/vincular` agregado por .NET.
- El prompt del agente menciona `/vincular` solo como instrucción condicional
  para solicitudes privadas.
- El modo invitado continúa sin ejecutar router o módulos y sin publicación
  global.
- El flujo vinculado y el flujo OTP conservan sus pruebas actuales.

## Siguiente incremento

`feature/pet-profile-read-module` registrará el primer módulo ejecutable de
solo lectura. Consumirá `GET /api/pets/mine` mediante un puerto neutral y el JWT
delegado; las escrituras y el registro de cuentas permanecerán fuera de alcance.
