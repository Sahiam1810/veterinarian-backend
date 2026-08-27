# JWT RS256 Signing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sustituir la firma simétrica HS256 por firma RSA RS256 para que .NET emita JWT con clave privada y otros servicios los validen únicamente con la clave pública.

**Architecture:** `JwtOptions` recibirá las dos claves PEM codificadas en Base64 y un `KeyId`. Un singleton `JwtRsaKeyMaterial` importará y poseerá las instancias RSA; `JwtTokenIssuer` consumirá la clave privada y JwtBearer la pública, con RS256 como único algoritmo permitido.

**Tech Stack:** .NET 10, ASP.NET Core JwtBearer, System.IdentityModel.Tokens.Jwt, Microsoft.IdentityModel.Tokens y xUnit.

## Global Constraints

- Trabajar en `refactor/jwt-rs256-signing` dentro del repositorio actual, sin worktree.
- Mantener login, refresh, claims, issuer, audience y tiempos de vida existentes.
- No utilizar Oracle en las pruebas.
- No agregar claves reales, tokens ni otros secretos al repositorio o a la salida de pruebas.
- Mantener todas las pruebas bajo `tests/` y usar TDD.
- Aceptar exclusivamente RS256; rechazar HS256 aunque issuer y audience coincidan.
- Exigir RSA de al menos 2048 bits y que las claves pública y privada correspondan.
- Usar commits convencionales.

---

### Task 1: Opciones y material RSA validado

**Files:**
- Modify: `src/Infrastructure/Security/Options/JwtOptions.cs`
- Modify: `src/Infrastructure/Security/Options/JwtOptionsValidator.cs`
- Create: `src/Infrastructure/Security/Tokens/JwtRsaKeyMaterial.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Create: `tests/Api.Tests/Security/JwtOptionsValidatorTests.cs`
- Create: `tests/Api.Tests/Support/RsaTestKeys.cs`

**Interfaces:**
- Produces: `JwtOptions.PrivateKeyPemBase64`, `PublicKeyPemBase64`, `KeyId`; `JwtRsaKeyMaterial.SigningKey` y `ValidationKey`, ambas `RsaSecurityKey` con el mismo `KeyId`.
- Consumes: PEM PKCS#8 privado y SubjectPublicKeyInfo público, codificados mediante `Convert.ToBase64String(Encoding.UTF8.GetBytes(pem))`.

- [ ] **Step 1: Crear claves RSA solamente en memoria para las pruebas**

`RsaTestKeys.Create(int keySize = 2048)` debe usar `RSA.Create(keySize)`, exportar `ExportPkcs8PrivateKeyPem()` y `ExportSubjectPublicKeyInfoPem()`, convertir ambos textos a Base64 y no imprimirlos.

- [ ] **Step 2: Escribir pruebas rojas del validador**

Cubrir con valores literales y claves generadas:

```csharp
Assert.True(validator.Validate(null, ValidOptions()).Succeeded);
Assert.True(validator.Validate(null, OptionsWithMismatchedKeys()).Failed);
Assert.True(validator.Validate(null, OptionsWithKeySize(1024)).Failed);
Assert.True(validator.Validate(null, OptionsWithInvalidBase64()).Failed);
Assert.True(validator.Validate(null, OptionsWithEmptyKeyId()).Failed);
```

La mutación que deben detectar es aceptar configuración incapaz de emitir y validar el mismo JWT RS256.

- [ ] **Step 3: Ejecutar el RED**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release --filter FullyQualifiedName~JwtOptionsValidatorTests
```

Expected: fallo de compilación porque las propiedades RSA y `JwtRsaKeyMaterial` aún no existen.

- [ ] **Step 4: Implementar opciones, validación y ownership de RSA**

Reemplazar `SigningKey` por:

```csharp
public string PrivateKeyPemBase64 { get; init; } = string.Empty;
public string PublicKeyPemBase64 { get; init; } = string.Empty;
public string KeyId { get; init; } = string.Empty;
```

El validador debe decodificar Base64, importar PEM con `RSA.ImportFromPem`, exigir `KeySize >= 2048` y comparar `Modulus` y `Exponent` públicos mediante `CryptographicOperations.FixedTimeEquals`. Debe capturar `FormatException`, `ArgumentException` y `CryptographicException` y retornar errores sin incluir las claves.

`JwtRsaKeyMaterial` implementará `IDisposable`, poseerá dos instancias RSA y expondrá:

```csharp
public RsaSecurityKey SigningKey { get; }
public RsaSecurityKey ValidationKey { get; }
```

Ambas tendrán `KeyId = options.Value.KeyId`. Registrarlo como singleton antes de `JwtTokenIssuer`.

- [ ] **Step 5: Ejecutar GREEN y commit**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release --filter FullyQualifiedName~JwtOptionsValidatorTests
git diff --check
git add src/Infrastructure/Security/Options src/Infrastructure/Security/Tokens/JwtRsaKeyMaterial.cs src/Infrastructure/DependencyInjection.cs tests/Api.Tests/Security/JwtOptionsValidatorTests.cs tests/Api.Tests/Support/RsaTestKeys.cs
git commit -m "refactor: configure RSA JWT key material"
```

### Task 2: Emisión RS256

**Files:**
- Modify: `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`
- Create: `tests/Api.Tests/Security/JwtTokenIssuerTests.cs`

**Interfaces:**
- Consumes: `JwtRsaKeyMaterial.SigningKey` y `JwtOptions.KeyId`.
- Produces: access token con header `alg=RS256`, `kid` configurado y los claims existentes sin cambios.

- [ ] **Step 1: Escribir pruebas rojas de emisión**

Instanciar el emisor con claves generadas y un `TimeProvider` fijo. Leer el token sin validarlo y comprobar literalmente:

```csharp
Assert.Equal(SecurityAlgorithms.RsaSha256, token.Header.Alg);
Assert.Equal(options.KeyId, token.Header.Kid);
Assert.Equal(identity.UserAccountId.ToString(), token.Subject);
Assert.Equal(identity.Role, token.Claims.Single(c => c.Type == "role").Value);
```

Validar además la firma con la clave pública y comprobar que la clave pública no puede usarse para emitir.

- [ ] **Step 2: Ejecutar RED**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release --filter FullyQualifiedName~JwtTokenIssuerTests
```

Expected: el token aún informa HS256 o el constructor no acepta `JwtRsaKeyMaterial`.

- [ ] **Step 3: Implementar la firma mínima**

Eliminar `SymmetricSecurityKey`, `Encoding.UTF8` y `HmacSha256`. Inyectar `JwtRsaKeyMaterial` y usar:

```csharp
new SigningCredentials(keyMaterial.SigningKey, SecurityAlgorithms.RsaSha256)
```

- [ ] **Step 4: Ejecutar GREEN y commit**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release --filter FullyQualifiedName~JwtTokenIssuerTests
git diff --check
git add src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs tests/Api.Tests/Security/JwtTokenIssuerTests.cs
git commit -m "refactor: sign JWT access tokens with RS256"
```

### Task 3: Validación Bearer RS256 de extremo HTTP

**Files:**
- Modify: `src/Api/Extensions/JwtAuthenticationExtensions.cs`
- Modify: `tests/Api.Tests/Common/Errors/ValidationExceptionHttpTests.cs`
- Create: `tests/Api.Tests/Security/JwtBearerAuthenticationTests.cs`
- Create: `tests/Api.Tests/AssemblyInfo.cs`

**Interfaces:**
- Consumes: `JwtRsaKeyMaterial.ValidationKey`, issuer, audience y clock skew.
- Produces: `TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256]` y autenticación HTTP sin Oracle.

- [ ] **Step 1: Evitar carreras de variables de entorno en pruebas alojadas**

Agregar:

```csharp
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

Actualizar el fixture existente para configurar `Jwt__PrivateKeyPemBase64`, `Jwt__PublicKeyPemBase64` y `Jwt__KeyId` desde `RsaTestKeys`; retirar `Jwt__SigningKey`.

- [ ] **Step 2: Escribir pruebas HTTP rojas**

Levantar `WebApplicationFactory` con un fake de `IAuthenticationService` que devuelva un `CurrentProfile` fijo para `/api/auth/me`. Crear tokens sin usar el emisor bajo prueba y verificar:

```text
RS256 + clave/issuer/audience correctos -> 200
otra clave -> 401
HS256 -> 401
issuer incorrecto -> 401
audience incorrecta -> 401
token expirado -> 401
token malformado -> 401
```

No afirmar sobre llamadas del fake; afirmar sobre el contrato HTTP y el perfil retornado.

- [ ] **Step 3: Ejecutar RED**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release --filter FullyQualifiedName~JwtBearerAuthenticationTests
```

Expected: el token RS256 válido recibe 401 porque JwtBearer todavía usa la clave simétrica.

- [ ] **Step 4: Configurar validación pública y allowlist**

Inyectar `JwtRsaKeyMaterial` en la configuración de `JwtBearerOptions`, reemplazar `SymmetricSecurityKey` por `keyMaterial.ValidationKey` y agregar:

```csharp
ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
```

Mantener issuer, audience, lifetime, clock skew, claims y eventos 401/403 existentes.

- [ ] **Step 5: Ejecutar GREEN y commit**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release --filter FullyQualifiedName~JwtBearerAuthenticationTests
dotnet test tests/Api.Tests/Api.Tests.csproj --configuration Release
git diff --check
git add src/Api/Extensions/JwtAuthenticationExtensions.cs tests/Api.Tests
git commit -m "refactor: validate bearer tokens with RS256"
```

### Task 4: Configuración pública y verificación global

**Files:**
- Modify: `.env.example`
- Verify: `src/`, `tests/`, tracked configuration and git history for this branch.

**Interfaces:**
- Produces: contrato de variables de entorno consumible por .NET y posteriormente por el agente, sin material real.

- [ ] **Step 1: Actualizar el ejemplo seguro**

Reemplazar `Jwt__SigningKey` por:

```dotenv
Jwt__PrivateKeyPemBase64=
Jwt__PublicKeyPemBase64=
Jwt__KeyId=
```

- [ ] **Step 2: Ejecutar verificación completa**

Run:

```powershell
dotnet test veterinarian_backend.slnx --configuration Release
dotnet build veterinarian_backend.slnx --configuration Release --no-restore
rg -n "Jwt__SigningKey|Jwt:SigningKey|HmacSha256|SymmetricSecurityKey" src tests .env.example
git diff --check
git status --short
```

Expected: todas las pruebas pasan, build sin errores, la búsqueda no encuentra uso activo de HS256 y solo aparecen cambios previstos.

- [ ] **Step 3: Revisar secretos y crear commit**

Run:

```powershell
git diff --cached --name-only
git diff -- .env.example
git add .env.example
git commit -m "docs: document RS256 environment variables"
```
