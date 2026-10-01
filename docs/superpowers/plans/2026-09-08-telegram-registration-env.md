# Telegram Registration Environment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable Telegram registration safely in development and keep its completion URL synchronized with the current Cloudflare Quick Tunnel.

**Architecture:** The ignored `.env` owns local secrets and feature state. The existing tunnel script remains the single operator entrypoint and updates both public Telegram URLs from one discovered tunnel base URL.

**Tech Stack:** PowerShell, ASP.NET Core configuration, xUnit, .NET 10

## Global Constraints

- Never print or commit `.env` values.
- Generate each OTP pepper from 32 cryptographically random bytes encoded as Base64.
- Preserve provider credentials and seeded catalog IDs.
- Work only on `fix/pet-photo-migration`.

---

### Task 1: Synchronize Telegram tunnel URLs

**Files:**
- Modify: `scripts/start-telegram-cloudflare-tunnel.ps1`
- Test: existing script checks plus static assertions

**Interfaces:**
- Consumes: the Quick Tunnel base URL discovered by the script.
- Produces: synchronized `Telegram__PublicWebhookUrl` and `Telegram__RegistrationCompletionUrl` entries.

- [x] **Step 1: Establish the failing static assertion**

Run a PowerShell assertion requiring the script to reference `Telegram__RegistrationCompletionUrl`; expect failure before implementation.

- [x] **Step 2: Implement the minimal synchronization**

Reuse the existing safe `.env` update helper and set the completion URL to `$publicUrl.TrimEnd('/') + '/telegram/registration/complete'`.

- [x] **Step 3: Verify the script**

Parse it with the PowerShell parser and rerun the static assertion; expect success with zero syntax errors.

### Task 2: Complete private development configuration

**Files:**
- Modify (ignored): `.env`

**Interfaces:**
- Consumes: current valid Quick Tunnel base URL.
- Produces: enabled registration, a matching completion URL, and isolated appointment/contact OTP peppers.

- [x] **Step 1: Generate protected values**

Generate two independent 32-byte values using `RandomNumberGenerator.Fill` and encode each as Base64.

- [x] **Step 2: Update only approved keys**

Set registration enabled, derive the completion URL, and fill the two optional dedicated peppers while preserving every other line.

- [x] **Step 3: Validate without revealing values**

Assert key presence, HTTPS URL shape, exact decoded pepper sizes, seeded Agent ID equality, and Git ignore status.

### Task 3: Runtime verification

**Files:**
- No production file changes.

**Interfaces:**
- Consumes: completed environment configuration.
- Produces: fresh evidence that options validation and the API host start successfully.

- [x] **Step 1: Run focused tests**

Run `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~TelegramOptionsValidatorTests` and the Telegram startup test.

- [x] **Step 2: Start and stop the API deterministically**

Run `dotnet run --no-build --project src/Api/Api.csproj --launch-profile http`, wait for the listening message, confirm no options validation error, then terminate the process.

- [x] **Step 3: Inspect repository state**

Run `git diff --check`, `git status --short`, and confirm `.env` is not tracked.

### Task 4: Isolate test hosts from the developer `.env`

**Files:**
- Modify: `src/Api/Program.cs`
- Test: existing hosted API tests

**Interfaces:**
- Consumes: `builder.Environment.EnvironmentName`.
- Produces: `.env` loading for development/production hosts and isolated configuration for `Testing` hosts.

- [x] **Step 1: Reproduce the failure**

Run the full suite with local Telegram registration enabled; hosted tests that set `Telegram__Enabled=false` fail because the process-global `.env` still supplies `Telegram__RegistrationEnabled=true`.

- [x] **Step 2: Apply the minimal source fix**

Create the builder first, skip `DotEnvLoader.Load()` for `Testing`, and re-add environment variables after loading `.env` for normal hosts.

- [x] **Step 3: Verify one previously failing hosted test**

Run `AppointmentOtpRateLimitHttpTests.RequestCode_Returns429_WithRateLimitExceeded_WhenPermitLimitIsExceeded`; expect success.

- [x] **Step 4: Run the full suite**

Run `dotnet test veterinarian_backend.slnx --no-restore`; expect 1081 passing tests and zero failures.
