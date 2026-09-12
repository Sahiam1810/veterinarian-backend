# Recordatorio Telegram de cita (1 hora antes)

## Goal

Avisar por Telegram al dueño, una hora antes de la cita, que debe llegar 10 minutos antes, sin cambiar el recordatorio de 24 h del panel ni tocar frontend o chatbot.

## Decisiones aprobadas

- Destinatario: solo el dueño, si tiene Telegram vinculado.
- Sin vínculo (o vínculo revocado): no enviar; persistir registro interno `SinVinculo`; no reintentar.
- Citas elegibles: `AGENDADA` y `CONFIRMADA`. Ignorar `CANCELADA`, `ATENDIDA`, `EN_PROGRESO` y `NO_ASISTIO`.
- Texto: `Recordatorio: {mascota} tiene cita el {dd/MM/yyyy HH:mm}. Por favor llega 10 minutos antes.`
- Hora en `America/Bogota`.
- Canal: Bot API `sendMessage` vía `ITelegramBotClient` en .NET. El chatbot Python no participa.
- El worker SignalR de 24 h (`Recordatorio`) permanece intacto.

## Architecture

La funcionalidad vive solo en `veterinarian-backend`. Un worker nuevo, separado del de 24 h, dispara un caso de uso nuevo. No hay tabla nueva, endpoint HTTP, pantalla ni módulo de chatbot.

```text
Worker .NET  →  citas a ~1 h  →  vínculo Telegram del dueño
                    │
                    ├─ vinculado     → sendMessage → NOTIFICATIONS (Enviado)
                    └─ sin vínculo   → NO envía    → NOTIFICATIONS (SinVinculo)

Recordatorio 24 h / SignalR  →  intacto
Frontend / ChatBot           →  intactos
```

Si `Telegram:Enabled=false` o `TelegramReminders:Enabled=false`, el worker no envía ni escribe.

## Components

| Pieza | Rol |
| --- | --- |
| `TelegramReminderOptions` | `Enabled=true`, `LeadMinutes=60`, `GraceMinutes=10` (ventana 50–70 min), `PollIntervalMinutes=5`, `AllowedStatusNames=AGENDADA,CONFIRMADA` |
| `TelegramAppointmentReminderBackgroundService` | Worker aparte |
| `DispatchTelegramAppointmentRemindersCommand` | Caso de uso que el worker dispara |

Reutiliza: `GetScheduledBetweenAsync`, `INotificationRepository`, `ITelegramUserLinkRepository`, `ITelegramBotClient`, `IAppointmentBookingSettings`.

Tipo de notificación: `Recordatorio1h` (independiente de `Recordatorio`, máximo 20 caracteres).

## Data flow

1. Ventana UTC: `now + (Lead - Grace)` … `now + (Lead + Grace)` (50–70 min con los valores por defecto).
2. Filtrar por estados permitidos.
3. Excluir citas que ya tengan `Recordatorio1h` (`GetNotifiedAppointmentIdsAsync`).
4. Dueño = `ClientPet.Client.UserId`. Vínculo activo: `TelegramUserLink` con `PersonId` igual al dueño y `UnlinkedAt == null`.
5. Con vínculo: `SendTextAsync` y, si responde, persistir `Enviado`. Persistencia **después** del envío, por cita, para no reenviar si Oracle confirma.
6. Sin vínculo, sin dueño o sin mascota: no llama a Telegram; persiste `SinVinculo`; no reintenta.
7. Fallo de Telegram: no persiste; queda para el siguiente ciclo mientras siga en ventana.
8. Un fallo no detiene el resto del lote.
9. Este flujo **no** llama a SignalR.

Una entrega Telegram por cita. Si reprograman antes de la ventana, el aviso usa la hora nueva. Si reprograman después de `Enviado`, no hay segundo mensaje.

## Error handling

- Worker o Telegram apagados: no-op.
- Estados no vigentes: ignore.
- API Telegram (red, 4xx/5xx, timeout): log sin PII (`AppointmentId`, conteos). No persiste. Reintenta en ventana.
- Excepción del worker: se captura, se loguea y el timer sigue.
- Idempotencia: cualquier `Recordatorio1h` existente (`Enviado` o `SinVinculo`) bloquea un nuevo envío.

## Testing

Unitarias del comando, sin red ni Oracle:

1. AGENDADA a ~60 min + vínculo → un `SendTextAsync` con hora Bogotá y “10 minutos antes”; `Enviado`.
2. Sin vínculo → cero envíos; `SinVinculo`.
3. Ya existe `Recordatorio1h` → no reenvía.
4. Solo existe `Recordatorio` de 24 h → sí envía Telegram.
5. CANCELADA / ATENDIDA / EN_PROGRESO / NO_ASISTIO → no envía ni guarda.
6. Fuera de ventana → no-op.
7. `SendTextAsync` lanza → no persiste.
8. Las pruebas del handler de 24 h siguen pasando.

No hay E2E contra BotFather. Frontend y chatbot no cambian.
