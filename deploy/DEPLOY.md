# Huellitas — despliegue Docker (producción)

Guía operativa sin secretos. El Compose central es `docker-compose.prod.yml` en este directorio (`veterinarian-backend/deploy/`).

## Dominios públicos

| Rol | URL |
| --- | --- |
| Frontend | https://huellitas.chatcampuslands.com |
| API | https://api.huellitas.chatcampuslands.com |

El **Nginx del host** termina TLS en 80/443 y hace proxy a los puertos loopback publicados abajo. **No** se configura el Nginx del host en este repositorio.

## Arquitectura Compose

Red privada única: `huellitas_network` (sin redes external ni recursos de otros proyectos).

| Servicio DNS | Contenedor (ejemplo) | Publicado al host | Escucha interna |
| --- | --- | --- | --- |
| `frontend` | `huellitas_frontend` | `127.0.0.1:5181` → 80 | 80 |
| `backend` | `huellitas_backend` | `127.0.0.1:5233` → 8080 | 8080 |
| `chatbot` | `huellitas_chatbot` | *(ninguno)* | 8010 |
| `redis` | `huellitas_redis` | *(ninguno)* | 6379 |
| `qdrant` | `huellitas_qdrant` | *(ninguno)* | 6333 / 6334 |
| `oracle` | `huellitas_oracle` | *(ninguno)* | 1521 |

Volúmenes con prefijo `huellitas_`: `huellitas_oracle_data`, `huellitas_redis_data`, `huellitas_qdrant_data`.

### DNS interno

- backend → chatbot: `http://chatbot:8010`
- chatbot → backend: `http://backend:8080`
- chatbot → redis: `redis://redis:6379`
- chatbot → qdrant: `http://qdrant:6333`
- backend → oracle: `Data Source=//oracle:1521/FREEPDB1` (PDB/servicio `FREEPDB1`)

Público (navegador / Telegram):

- `VITE_API_URL=https://api.huellitas.chatcampuslands.com` (build arg del frontend)
- `Telegram__PublicWebhookUrl=https://api.huellitas.chatcampuslands.com`
- CORS producción: solo `https://huellitas.chatcampuslands.com`

## Layout en el VPS (siblings)

Los contextos de build asumen esta estructura (carpeta padre + tres repos):

```text
Huellitas/                         # carpeta padre (no es un git root obligatorio)
  veterinarian-backend/            # este repo
    .env                           # solo en VPS; no versionar
    deploy/
      docker-compose.prod.yml
      DEPLOY.md
      .env.prod.example
  veterinarian-fronted/
  Huellitas_ChatBot/
    .env                           # solo en VPS; no versionar
```

## Secretos y archivos `.env` (solo en VPS)

**Nunca versionar** `.env` reales. En el VPS (desde la carpeta padre `Huellitas/`):

```text
cp veterinarian-backend/.env.example veterinarian-backend/.env
cp Huellitas_ChatBot/.env.example Huellitas_ChatBot/.env
# Editar valores reales solo en el servidor
```

Compose carga (rutas relativas a `veterinarian-backend/deploy/`):

- `../.env` → servicios `backend` y `oracle` (la imagen Oracle solo usa `ORACLE_PASSWORD`, `APP_USER`, `APP_USER_PASSWORD`; el resto de claves del `.env` también quedan en el entorno del contenedor Oracle — mantener la red privada)
- `../../Huellitas_ChatBot/.env` → servicio `chatbot`

Los `env_file` usan `required: false` para poder validar el YAML sin archivos locales; **en el VPS deben existir** antes de `up`.

### Nombres de variables (valores solo en VPS)

**Backend / Oracle** (`veterinarian-backend/.env`):

- `ConnectionStrings__DefaultConnection` — host Docker: `//oracle:1521/FREEPDB1`; no hardcodear password en YAML
- `ORACLE_PASSWORD`, `APP_USER`, `APP_USER_PASSWORD` — imagen `gvenzl/oracle-free`
- `Jwt__PrivateKeyPemBase64`, `Jwt__PublicKeyPemBase64`, `Jwt__KeyId`, `Jwt__Issuer`, `Jwt__Audience`
- `Cors__AllowedOrigins__0`
- `Agent__Enabled` (`Agent__BaseUrl` lo fija el Compose prod)
- `Telegram__*` sensibles (`BotToken`, `WebhookSecret`, peppers, protection key, etc.)
- `Telegram__PublicWebhookUrl`
- `Email__*`, `SWAGGER_*`, peppers OTP, Twilio si aplica

**Chatbot** (`Huellitas_ChatBot/.env`):

- `HUELLITAS_JWT_PUBLIC_KEY_PEM_BASE64`, `HUELLITAS_JWT_ISSUER`, `HUELLITAS_JWT_AUDIENCE`, `HUELLITAS_JWT_KEY_ID`
- Claves del proveedor de chat / embeddings según configuración
- Opcionales: `HUELLITAS_QDRANT_API_KEY`, `HUELLITAS_REDIS_USERNAME`, `HUELLITAS_REDIS_PASSWORD`

Checklist de nombres: `.env.prod.example` en este directorio (solo placeholders).

## Validar Compose (sin build / sin up)

Desde la carpeta padre `Huellitas/`:

```powershell
docker compose -f .\veterinarian-backend\deploy\docker-compose.prod.yml config
```

Desde este directorio (`veterinarian-backend/deploy/`):

```powershell
docker compose -f .\docker-compose.prod.yml config
```

No se usan `${VAR}` a nivel Compose para secretos; no hace falta `--env-file` para validar. Si en el futuro se añaden interpolaciones, use:

```powershell
docker compose --env-file .\.env.prod.example -f .\docker-compose.prod.yml config
```

## Orden futuro de despliegue (manual; no automatizado aquí)

1. Crear `.env` reales en VPS a partir de los `.env.example`.
2. Asegurar que host Nginx apunta a `127.0.0.1:5181` (frontend) y `127.0.0.1:5233` (API).
3. `docker compose -f .\veterinarian-backend\deploy\docker-compose.prod.yml pull` / `build` según política del VPS (cwd = carpeta padre).
4. Arrancar Oracle y esperar healthy; **aplicar migraciones EF y seeds a mano** (ver abajo) **antes** de depender de la API con datos.
5. `docker compose -f .\veterinarian-backend\deploy\docker-compose.prod.yml up -d` (o arranque ordenado equivalente).
6. Verificar health del chatbot (`/health/live`, `/health/ready` vía red interna o diagnóstico en VPS).
7. Configurar webhook Telegram hacia la URL HTTPS pública de la API.

### Migraciones y seeds (manuales, primer deploy)

No se ejecutan desde este documento. En el backend (`veterinarian-backend/`), con Oracle alcanzable y connection string correcto:

```powershell
dotnet ef database update `
  --project .\src\Infrastructure\Infrastructure.csproj `
  --startup-project .\src\Api\Api.csproj `
  --context VeterinaryDbContext
```

Luego seeds idempotentes (`database/seeds`) según el README del backend. Provisionar SuperAdmin aparte.

## Compose locales por repo

| Archivo | Uso |
| --- | --- |
| `veterinarian-backend/deploy/docker-compose.prod.yml` | **Única** definición de producción del stack completo |
| `veterinarian-backend/docker-compose.yml` | Solo desarrollo local (oracle + backend) |
| `Huellitas_ChatBot/compose.yaml` | Solo desarrollo local (chatbot + redis + qdrant) |

Para stack completo alineado a prod, preferir el Compose en `deploy/` (con `.env` locales no versionados), no unir a mano redes entre composes de repo.

## Frontend

Build de producción inyecta `VITE_API_URL=https://api.huellitas.chatcampuslands.com`. El navegador **no** debe llamar a `http://backend:8080`. Vite local sigue en el puerto **5174**.
