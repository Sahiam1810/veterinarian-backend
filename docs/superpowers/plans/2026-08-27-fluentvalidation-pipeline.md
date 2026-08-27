# FluentValidation Pipeline Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Garantizar que todos los validadores registrados en Application se ejecuten automáticamente antes de los handlers de MediatR.

**Architecture:** `AddApplication` seguirá siendo el composition root de la capa y registrará `ValidationBehavior<,>` como comportamiento abierto. Una prueba de integración de Application resolverá `IMediator` desde el contenedor real, enviará `LoginCommand` y sustituirá únicamente el puerto externo `IAuthenticationService`, sin Oracle.

**Tech Stack:** .NET 10, MediatR 14, FluentValidation 12, xUnit y Microsoft.Extensions.DependencyInjection.

## Global Constraints

- Trabajar en `fix/fluentvalidation-pipeline` dentro del directorio actual, sin worktree.
- Centralizar las pruebas bajo `tests/`.
- No modificar Oracle, persistencia, JWT, controladores ni reglas de validación existentes.
- Usar TDD y commits convencionales.

---

### Task 1: Prueba de regresión del pipeline

**Files:**
- Create: `tests/Application.Tests/Application.Tests.csproj`
- Create: `tests/Application.Tests/Common/Validators/ValidationPipelineTests.cs`
- Modify: `veterinarian_backend.slnx`

**Interfaces:**
- Consumes: `Application.DependencyInjection.AddApplication()`, `IMediator.Send`, `LoginCommand` e `IAuthenticationService`.
- Produces: una prueba que demuestra que un login inválido no alcanza el servicio y uno válido sí lo alcanza.

- [ ] **Step 1: Crear el proyecto de pruebas bajo `tests/`**

Run:

```powershell
dotnet new xunit --framework net10.0 --output tests/Application.Tests
dotnet add tests/Application.Tests/Application.Tests.csproj reference src/Application/Application.csproj
dotnet sln veterinarian_backend.slnx add tests/Application.Tests/Application.Tests.csproj
```

- [ ] **Step 2: Escribir la prueba mediante el contenedor real**

Crear `ValidationPipelineTests` con un `ServiceCollection`, llamar `AddApplication()`, registrar un `FakeAuthenticationService` como `IAuthenticationService` y resolver `IMediator`. La primera prueba enviará:

```csharp
new LoginCommand("correo-invalido", "secret")
```

y verificará `Assert.ThrowsAsync<ValidationException>` junto con `LoginCalls == 0`. La segunda enviará:

```csharp
new LoginCommand("cliente@huellitas.test", "secret")
```

y verificará un resultado exitoso y `LoginCalls == 1`. El fake devolverá un `AuthenticationTokens` literal y sus otros métodos lanzarán `NotSupportedException`.

- [ ] **Step 3: Ejecutar la prueba y observar el fallo esperado**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --configuration Release
```

Expected: la prueba del correo inválido falla porque el comando llega al fake y no se lanza `ValidationException`.

### Task 2: Registro mínimo y verificación

**Files:**
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/Application.Tests/Common/Validators/ValidationPipelineTests.cs`

**Interfaces:**
- Consumes: `Application.Common.Validators.ValidationBehavior<TRequest,TResponse>`.
- Produces: registro abierto aplicado a todas las solicitudes MediatR que tengan validadores.

- [ ] **Step 1: Registrar el comportamiento abierto**

Importar `Application.Common.Validators` y ampliar el registro existente:

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
```

Mantener `AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>()` sin cambios.

- [ ] **Step 2: Ejecutar la prueba de regresión**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --configuration Release
```

Expected: ambas pruebas pasan; la inválida es detenida y la válida llega al servicio.

- [ ] **Step 3: Ejecutar toda la solución**

Run:

```powershell
dotnet test veterinarian_backend.slnx --configuration Release
dotnet build veterinarian_backend.slnx --configuration Release --no-restore
git diff --check
```

Expected: cero pruebas fallidas, cero errores de compilación y ningún error de whitespace.

- [ ] **Step 4: Revisar y crear el commit convencional**

Run:

```powershell
git diff -- src/Application/DependencyInjection.cs tests/Application.Tests veterinarian_backend.slnx
git add src/Application/DependencyInjection.cs tests/Application.Tests veterinarian_backend.slnx
git commit -m "fix: enable FluentValidation pipeline"
```
