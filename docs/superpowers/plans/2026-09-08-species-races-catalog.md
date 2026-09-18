# Species Races Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relacionar las razas con su especie, poblar un catálogo inicial y permitir que el agente consulte únicamente las razas válidas para la especie elegida.

**Architecture:** Evolucionar `RaceEntity` como dependiente requerido de `SpeciesEntity`, propagar el filtro opcional por Application/API y persistirlo mediante EF Core Oracle. Después adaptar el puerto HTTP del agente para enviar el `speciesId` guardado en el formulario.

**Tech Stack:** .NET 10, EF Core Oracle, MediatR, FluentValidation, ASP.NET Core, Python 3.12, httpx, pytest.

## Global Constraints

- Preservar la arquitectura de cuatro capas y las vertical slices existentes.
- Relación requerida `Species 1:N Races`, FK en `RACES`, borrado `Restrict`.
- Unicidad de nombre de raza dentro de cada especie.
- `GET /api/races` conserva el listado general y acepta `speciesId` opcional.
- No cambiar autenticación, permisos ni políticas existentes.
- Migración y seeds se aplican únicamente a Oracle local después de verificarlos.
- Ejecutar pruebas enfocadas, no suites masivas.

---

### Task 1: Dominio Race-Species

**Files:**
- Modify: `src/Domain/Races/Entities/RaceEntity.cs`
- Test: existing Domain/Application test project following repository placement

- [ ] Escribir una prueba fallida que exija `SpeciesId` al crear y actualizar una raza.
- [ ] Ejecutar la prueba y confirmar el fallo esperado.
- [ ] Añadir `SpeciesId`, navegación privada/solo lectura y validación de especie requerida.
- [ ] Ejecutar pruebas enfocadas y `dotnet build src/Domain/Domain.csproj -c Release`.
- [ ] Detenerse en el gate de Domain.

### Task 2: Casos de uso y repositorio

**Files:**
- Modify: `src/Application/Races/Abstraction/IRaceRepository.cs`
- Modify: `src/Application/Races/UseCases/GetAllRacesQuery.cs`
- Modify: `src/Application/Races/UseCases/CreateRaceCommand.cs`
- Modify: `src/Application/Races/UseCases/CreateRaceCommandValidator.cs`
- Modify: `src/Application/Races/UseCases/UpdateRaceCommand.cs`
- Modify: `src/Application/Races/UseCases/UpdateRaceCommandValidator.cs`
- Test: `tests/Application.Tests/Races/*`

- [ ] Escribir pruebas fallidas para filtro, existencia de especie y unicidad por especie.
- [ ] Ejecutar las pruebas y confirmar RED.
- [ ] Propagar `SpeciesId`, validar existencia y actualizar firmas del repositorio.
- [ ] Ejecutar pruebas enfocadas y build de Application.
- [ ] Detenerse en el gate de Application.

### Task 3: Persistencia, migración y seeds

**Files:**
- Modify: `src/Infrastructure/Races/Configuration/RacesConfiguration.cs`
- Modify: `src/Infrastructure/Races/Repositories/RaceRepository.cs`
- Create: `src/Infrastructure/Migrations/<timestamp>_AddRaceSpeciesRelationship.cs`
- Modify: `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs`
- Modify: `database/seeds/veterinary_catalogs_seed.sql`
- Modify: `database/seeds/verify_seeds.sql`
- Test: `tests/Infrastructure.Tests/Races/*`

- [ ] Escribir pruebas fallidas del modelo para FK requerida, índice compuesto y `Restrict`.
- [ ] Implementar configuración y consultas filtradas.
- [ ] Generar una migración EF, revisar que haga add/backfill/alter/index/FK sin drops.
- [ ] Añadir `MERGE` idempotente de las diez razas asociadas.
- [ ] Ejecutar pruebas enfocadas, build y `has-pending-model-changes`.
- [ ] Detenerse en el gate de Infrastructure antes de aplicar la base.

### Task 4: Contrato HTTP de razas

**Files:**
- Modify: `src/Api/Races/Dtos/RaceDtos.cs`
- Modify: `src/Api/Races/Mappings/RaceMappingsExtensions.cs`
- Modify: `src/Api/Races/Controllers/RacesController.cs`
- Test: `tests/Api.Tests/Races/*`

- [ ] Escribir pruebas fallidas para `speciesId` opcional en GET y requerido en POST/PUT.
- [ ] Actualizar DTOs, mappings, controller y OpenAPI manteniendo la autorización actual.
- [ ] Ejecutar pruebas enfocadas y build Release.
- [ ] Detenerse en el gate de API.

### Task 5: Consumidor del agente

**Files:**
- Modify: `src/app/ports/pet_profile_gateway.py`
- Modify: `src/app/adapters/dotnet/pet_profile.py`
- Modify: `src/app/modules/pet_profile/nodes/collect_pet_registration.py`
- Modify: test doubles implementing `list_races`
- Test: `tests/unit/adapters/dotnet/test_pet_profile_gateway.py`
- Test: `tests/unit/modules/pet_profile/*`

- [ ] Escribir pruebas fallidas que exijan `GET /api/races?speciesId=<id>`.
- [ ] Cambiar el puerto a `list_races(species_id, bearer_token)` y usar el ID del borrador.
- [ ] Actualizar dobles de prueba sin cambiar otros módulos.
- [ ] Ejecutar Ruff y las pruebas enfocadas del perfil/registro.

### Task 6: Aplicación local y verificación

**Files:**
- No production code changes expected.

- [ ] Cargar variables privadas sin imprimir sus valores.
- [ ] Ejecutar `dotnet ef database update` contra Oracle local.
- [ ] Ejecutar `database/seeds/apply_all.sql` con SQL*Plus.
- [ ] Consultar conteos por especie y verificar FK/índice.
- [ ] Repetir tests enfocados, build Release y `git diff --check` en ambos repositorios.
