# Repair AddPetPhotoUrl Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make EF Core discover and safely apply AddPetPhotoUrl to VET_APP, then seed and verify the active Oracle database.

**Architecture:** Preserve the existing migration identifier and snapshot. Add explicit EF metadata and idempotent Oracle blocks to the migration, while making the fallback patch operate on the connected application schema. Verify discovery with an Infrastructure test before applying database changes.

**Tech Stack:** .NET 10, EF Core 10, Oracle.EntityFrameworkCore, Oracle Database FREEPDB1, xUnit, SQL*Plus production seed scripts.

## Global Constraints

- Target only VET_APP on FREEPDB1.
- Preserve migration identifier 20260908150000_AddPetPhotoUrl.
- Do not run cleanup_seeds.sql or database/test_seeds scripts.
- Never print connection credentials.

---

### Task 1: Prove migration discovery

**Files:**
- Create: `tests/Infrastructure.Tests/Persistence/AddPetPhotoUrlMigrationTests.cs`
- Modify: `src/Infrastructure/Migrations/20260908150000_AddPetPhotoUrl.cs`

**Interfaces:**
- Consumes: `VeterinaryDbContext` and EF `IMigrationsAssembly`.
- Produces: discoverable migration key `20260908150000_AddPetPhotoUrl`.

- [ ] **Step 1: Write the failing test**

```csharp
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Tests.Persistence;

public sealed class AddPetPhotoUrlMigrationTests
{
    [Fact]
    public void MigrationAssembly_DiscoversAddPetPhotoUrl()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=test;Password=test;Data Source=test")
            .Options;
        using var context = new VeterinaryDbContext(options);

        var migrations = context.GetService<IMigrationsAssembly>().Migrations;

        Assert.Contains("20260908150000_AddPetPhotoUrl", migrations.Keys);
    }
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~AddPetPhotoUrlMigrationTests`

Expected: FAIL because the migration key is absent.

- [ ] **Step 3: Add migration metadata and idempotent Oracle DDL**

Add `DbContext` and `Migration` attributes to `AddPetPhotoUrl`. Replace direct AddColumn/DropColumn operations with Oracle blocks that query `USER_TAB_COLUMNS` before executing `ALTER TABLE PETS ADD/DROP COLUMN PHOTO_URL`.

- [ ] **Step 4: Run the focused test and verify GREEN**

Run the Step 2 command. Expected: PASS.

- [ ] **Step 5: Commit the migration repair**

```powershell
git add tests/Infrastructure.Tests/Persistence/AddPetPhotoUrlMigrationTests.cs src/Infrastructure/Migrations/20260908150000_AddPetPhotoUrl.cs
git commit -m "fix: register pet photo migration"
```

### Task 2: Correct the manual fallback patch

**Files:**
- Modify: `database/patches/add_pet_photo_url.sql`

**Interfaces:**
- Consumes: a session already connected as VET_APP to FREEPDB1.
- Produces: idempotent PHOTO_URL creation and migration-history registration in the current schema.

- [ ] **Step 1: Add a static failing check**

Run: `rg -n "TOMDEVV" database/patches/add_pet_photo_url.sql`

Expected: matches identifying the obsolete schema dependency.

- [ ] **Step 2: Make the patch schema-safe**

Use `USER_TAB_COLUMNS`, unqualified `PETS`, and the current schema's quoted `__EFMigrationsHistory`. Add a guard that raises an application error unless session user/current schema is VET_APP and container is FREEPDB1.

- [ ] **Step 3: Verify the obsolete schema is gone**

Run: `rg -n "TOMDEVV" database/patches/add_pet_photo_url.sql`

Expected: no matches.

- [ ] **Step 4: Commit the patch repair**

```powershell
git add database/patches/add_pet_photo_url.sql
git commit -m "fix: target active schema in pet photo patch"
```

### Task 3: Apply and verify database state

**Files:**
- No source changes.

**Interfaces:**
- Consumes: `.env` connection `ConnectionStrings__DefaultConnection` and production seed runner.
- Produces: migrated and seeded VET_APP schema.

- [ ] **Step 1: Build and test**

Run: `dotnet build veterinarian_backend.slnx` and `dotnet test veterinarian_backend.slnx --no-build`.

- [ ] **Step 2: Confirm EF discovery**

Run: `dotnet ef migrations list --project src/Infrastructure/Infrastructure.csproj --startup-project src/Api/Api.csproj --no-build`.

Expected: includes `20260908150000_AddPetPhotoUrl` as pending or applied.

- [ ] **Step 3: Apply EF migration**

Run: `dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/Api/Api.csproj --no-build`.

Expected: migration applied or already applied without duplicate-column failure.

- [ ] **Step 4: Execute production seeds**

Run `database/seeds/apply_all.sql` against the same VET_APP@FREEPDB1 connection. Do not execute cleanup or test seeds.

- [ ] **Step 5: Verify final state**

Confirm one PHOTO_URL column, one migration-history row, and all minimum catalog counts documented in `database/seeds/README.md`.

- [ ] **Step 6: Check repository state**

Run: `git status --short` and `git diff --check`.
