# Etapa 5 — exit gate (portal Cliente JWT)

Checklist de cierre segun ADR `docs/adr/2026-09-07-etapa-5-client-portal-retirement.md`.

## Pipeline 410

- [ ] `GoneException` + `ClientPortal.Gone` responde HTTP 410 `application/problem+json` con `code`.
- [ ] No se usa 403/404 para rutas de portal retiradas.

## Telefono de citas

- [ ] Create/Update staff y CreateMy usan telefono del perfil del dueno cuando existe.
- [ ] Sin telefono en perfil, el request aporta el valor (Create).

## Rutas portal → 410 (AllowAnonymous stub)

- [ ] `GET /api/clients/me`
- [ ] `GET/POST /api/appointments/mine*`, `GET booking/options`, `GET booking/slots`
- [ ] `PATCH /api/appointments/mine/{id}/cancel`
- [ ] `GET/POST/PATCH /api/pets/mine*`
- [ ] `GET /api/vaccinations/mine`
- [ ] `GET /api/accountstatements/mine`

## OTP citas (NO retirar)

- [ ] `POST .../request-code` y `.../confirm-code` siguen `AllowAnonymous` + rate limit.

## Permisos Cliente

- [ ] Seed `role_permissions_seed.sql`: rol Cliente sin grants de modulos; DELETE residuales.
- [ ] `verify_seeds.sql` cuenta 0 filas para ROLE_ID Cliente.

## Policy

- [ ] `AuthorizationPolicies.ClientOnly` eliminada (sin consumidores).
- [ ] `GET /api/appointments/me` (veterinario) intacto.
- [ ] Sin WhatsApp en Etapa 5.