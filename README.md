# Veterinarian Backend

Backend modular de Huellitas construido con ASP.NET Core y Oracle. La API
autentica usuarios mediante JWT RS256 y expone módulos independientes por
capacidad del negocio.

## Configuración local

1. Copie `.env.example` como `.env`.
2. Configure `ConnectionStrings__DefaultConnection` para Oracle.
3. Configure las claves y metadatos `Jwt__*`.
4. Mantenga los secretos únicamente en `.env`; ese archivo no debe subirse al
   repositorio.

Para iniciar la API:

```powershell
dotnet run --project src/Api/Api.csproj --launch-profile http
```

En ambiente `Development`, Swagger está disponible en la URL mostrada por
ASP.NET Core, agregando `/swagger`.

## Gateway del agente conversacional

El módulo `Agent` expone `POST /api/agent/messages`. El cliente llama solamente
al backend .NET; el backend deriva `person_id` y `role` del JWT validado y
reenvía internamente la solicitud a Huellitas ChatBot.

Configure estas variables en `.env`:

```dotenv
Agent__Enabled=true
Agent__BaseUrl=http://localhost:8000
Agent__MessagesPath=/api/v1/messages
Agent__RequestTimeoutSeconds=30
Agent__ConversationContextTtlSeconds=900
Agent__ConversationContextCapacity=10000
Agent__MaxResponseBytes=1048576
```

Cuando backend y chatbot estén en la misma red de Docker, use el nombre DNS del
servicio en lugar de `localhost`, por ejemplo:

```dotenv
Agent__BaseUrl=http://agent-api:8000
```

### Prueba desde Swagger

1. Inicie Huellitas ChatBot y Qdrant con `docker compose` desde el repositorio
   `Huellitas_ChatBot`.
2. Inicie este backend con `Agent__Enabled=true`.
3. Obtenga un access token mediante `POST /api/auth/login` o
   `POST /api/auth/register`.
4. Autorice Swagger con el access token.
5. Ejecute `POST /api/agent/messages` y agregue un valor único en el header
   `Idempotency-Key`.
6. Reutilice el `conversationId` retornado en los mensajes siguientes del mismo
   hilo.

Solicitud inicial:

```json
{
  "message": "¿Qué vacunas necesita mi mascota?",
  "conversationId": null,
  "petId": null,
  "language": "es-CO"
}
```

El contrato público no permite enviar `userId`, `roles`, `channel`,
`isEscalated` ni `publishAsGlobalKnowledge`. Esos valores son controlados por
el backend.

Por ahora, los identificadores generados se mantienen en memoria con TTL y
capacidad limitada. No representan historial canónico y se pierden al reiniciar
la API. El futuro módulo especializado de conversaciones reemplazará este
proveedor mediante inyección de dependencias, sin modificar el endpoint ni el
caso de uso de `Agent`.
