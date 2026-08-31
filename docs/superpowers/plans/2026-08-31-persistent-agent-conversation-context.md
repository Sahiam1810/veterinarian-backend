# Persistent Agent Conversation Context Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist the conversation and authenticated client participant before forwarding a message to the chatbot, and authorize subsequent access from the stored participant relationship.

**Architecture:** Keep `POST /api/agent/messages` and `IConversationContextProvider` stable. Replace the transient Infrastructure implementation with Application orchestration over the existing Unit of Work and domain factories; Infrastructure supplies validated catalog defaults and scoped dependencies. No schema migration or new endpoint is required.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core Oracle, MediatR, options validation, xUnit, NSubstitute (tests only), Oracle SQL.

## Global Constraints

- Work on `feature/persistent-agent-conversation-context` without a worktree.
- Derive identity only from JWT `person_id`.
- Commit profile, conversation, and participant with one `SaveChangesAsync`.
- Do not persist messages or create AI/human participants.
- Do not apply migrations or seeds automatically.
- Keep JWT, messages, and personal data out of logs.
- Run targeted tests at gates; use Conventional Commits with emoji.

---

## Approved contract and layer impact

- Mode: `add-use-case` spanning existing Agent, ChatConversations, ChatUserProfiles, and ChatParticipants modules.
- Trigger: existing authenticated `POST /api/agent/messages`.
- Output: `AgentConversationContext(ConversationId, "web", IsEscalated)`.
- Supplied missing conversation: `404`; conversation not owned by JWT user: `403`; missing catalog: `503`.
- Domain: unchanged because existing factories enforce the required invariants.
- Application: persistent orchestration, defaults port, and typed exceptions.
- Infrastructure: seed, options, scoped registration, and removal of the transient provider.
- API: error mapping and OpenAPI metadata; request/success DTOs remain compatible.
- Idempotency: the key remains forwarded, but durable first-message mapping is deferred because the schema has nowhere to store it.

## Task 1: Seed and catalog configuration

**Files:**
- Create: `database/seeds/chat_conversation_catalogs_seed.sql`
- Modify: `src/Infrastructure/Agent/Configuration/AgentOptions.cs`
- Modify: `src/Infrastructure/Agent/Configuration/AgentOptionsValidator.cs`
- Modify: `tests/Infrastructure.Tests/Agent/Configuration/AgentOptionsValidatorTests.cs`
- Modify: `.env.example`, `README.md`

**Produces:** `InitialConversationStatusId` and `ClientParticipantTypeId`; IDs `81000000-0000-0000-0000-000000000001` (Abierta) and `82000000-0000-0000-0000-000000000001` (Cliente).

- [ ] Add failing tests rejecting empty, malformed, and all-zero GUIDs while preserving disabled-Agent startup.
- [ ] Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentOptionsValidatorTests" --logger "console;verbosity=minimal"`.
- [ ] Confirm RED because the properties/validation do not exist.
- [ ] Add both string properties and validate with `Guid.TryParse(value, out var id) && id != Guid.Empty` when Agent is enabled.
- [ ] Create idempotent Oracle `MERGE` statements:

```sql
MERGE INTO CONVERSATIONS_STATUSES t
USING (SELECT '81000000-0000-0000-0000-000000000001' ID FROM DUAL) s
ON (t.CONVERSATIONS_STATUSES_ID = s.ID)
WHEN NOT MATCHED THEN INSERT (CONVERSATIONS_STATUSES_ID, NAME_STATUS, CREATED_AT)
VALUES (s.ID, 'Abierta', SYSTIMESTAMP);

MERGE INTO SENDER_TYPES t
USING (SELECT '82000000-0000-0000-0000-000000000001' ID FROM DUAL) s
ON (t.SENDER_TYPES_ID = s.ID)
WHEN NOT MATCHED THEN INSERT (SENDER_TYPES_ID, NAME_TYPE, CREATED_AT)
VALUES (s.ID, 'Cliente', SYSTIMESTAMP);
COMMIT;
```

- [ ] Add the two `Agent__*` values to `.env.example` and seed execution instructions to README.
- [ ] Re-run the targeted test and confirm GREEN.
- [ ] Commit: `feat: ✨ configure agent conversation catalogs`.

**Gate:** report Task 1 evidence and request approval before Application edits.

## Task 2: Persistent Application orchestration

**Files:**
- Create: `src/Application/Agent/Abstractions/IAgentConversationDefaults.cs`
- Create: `src/Application/Agent/Conversations/PersistentConversationContextProvider.cs`
- Modify: `src/Application/Agent/Errors/AgentGatewayExceptions.cs`
- Modify: `tests/Application.Tests/Application.Tests.csproj` (NSubstitute 5.3.0, test only)
- Create: `tests/Application.Tests/Agent/Conversations/PersistentConversationContextProviderTests.cs`

**Produces:**

```csharp
public interface IAgentConversationDefaults
{
    Guid InitialConversationStatusId { get; }
    Guid ClientParticipantTypeId { get; }
}
```

- [ ] Write failing tests for: missing profile creates it; existing profile is reused; conversation and client participant are added; exactly one save occurs.
- [ ] Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~PersistentConversationContextProviderTests" --logger "console;verbosity=minimal"`.
- [ ] Confirm RED because the provider/defaults/exceptions do not exist.
- [ ] Implement the new-conversation path using `UsersRepository.GetByIdAsync`, `ChatUserProfilesRepository.GetByUserIdAsync`, both catalog repositories, `ChatConversation.Create`, and `ChatParticipant.Create`.
- [ ] Add all entities before one `SaveChangesAsync`; return the generated ID, channel `web`, and `false` escalation.
- [ ] Confirm GREEN for creation tests.
- [ ] Add failing tests for: owned existing conversation; missing conversation; foreign conversation; any owned profile among multiple profiles; unresolved escalation; resolved escalation; cancellation propagation.
- [ ] Implement existing-conversation authorization by comparing participant `ChatUserProfileId` values with all profiles owned by `personId`.
- [ ] Determine escalation as active when an escalation has no records from `ChatEscalationResolutionsRepository.GetByChatEscalationIdAsync`.
- [ ] Run focused Application tests:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~PersistentConversationContextProviderTests|FullyQualifiedName~Agent.Messages|FullyQualifiedName~ChatConversations|FullyQualifiedName~ChatParticipants|FullyQualifiedName~ChatUserProfiles" --logger "console;verbosity=minimal"
```

- [ ] Commit: `feat: ✨ persist agent conversation context`.

**Gate:** report Application evidence and request approval before Infrastructure wiring.

## Task 3: Scoped Infrastructure wiring

**Files:**
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Delete: `src/Infrastructure/Agent/Conversations/TransientConversationContextProvider.cs`
- Delete: `tests/Infrastructure.Tests/Agent/Conversations/TransientConversationContextProviderTests.cs`
- Create: `tests/Infrastructure.Tests/Agent/AgentDependencyInjectionTests.cs`
- Modify: old TTL/capacity settings in options, validator, `.env.example`, and README.

- [ ] Write a failing DI test asserting `IConversationContextProvider` is scoped and implemented by `PersistentConversationContextProvider`.
- [ ] Assert scoped `IAgentConversationDefaults` resolves both parsed GUIDs.
- [ ] Run the DI test and confirm RED while the singleton transient provider remains.
- [ ] Register scoped defaults from validated `IOptions<AgentOptions>` and register the persistent provider as scoped.
- [ ] Remove transient provider code/tests and obsolete context TTL/capacity settings.
- [ ] Run targeted DI/options tests and `dotnet build veterinarian_backend.slnx --no-restore`.
- [ ] Require zero selected-test failures, zero build errors, and zero warnings.
- [ ] Commit: `refactor: ♻️ replace transient conversation context`.

**Gate:** report Infrastructure evidence and request approval before API edits.

## Task 4: Safe API errors

**Files:**
- Modify: `src/Api/Common/Errors/GlobalExceptionHandler.cs`
- Modify: `src/Api/Agent/Controllers/AgentMessagesController.cs`
- Modify: `tests/Api.Tests/Agent/AgentMessagesHttpTests.cs`

- [ ] Make the API test context provider able to throw a configured exception.
- [ ] Write failing tests for `AgentConversationNotFoundException -> 404/agent_conversation_not_found`, `AgentConversationForbiddenException -> 403/agent_conversation_forbidden`, and `AgentConversationConfigurationException -> 503/agent_conversation_configuration_error`.
- [ ] Assert problem JSON contains no secret or internal identifier and OpenAPI lists 403/404.
- [ ] Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentMessagesHttpTests" --logger "console;verbosity=minimal"` and confirm RED.
- [ ] Add explicit exception mappings and `[ProducesResponseType(StatusCodes.Status404NotFound)]` without changing authorization, request, route, or success response.
- [ ] Re-run and confirm GREEN.
- [ ] Commit: `feat: ✨ expose safe conversation access errors`.

**Gate:** report API evidence and request approval for final audit.

## Task 5: Final verification

- [ ] Run the three focused test commands from Tasks 2-4 once more.
- [ ] Run `dotnet build veterinarian_backend.slnx --no-restore`.
- [ ] Run `git diff --check develop...HEAD`, `git status --short`, `git diff --stat develop...HEAD`, and `git log --oneline develop..HEAD`.
- [ ] Verify one save for creation, participant ownership before chatbot invocation, scoped lifetimes, idempotent seed without credentials, no migration, and backward-compatible endpoint DTOs.
- [ ] Report live Oracle seed/execution as unverified until the user runs it manually.

## Remaining risks

- Retried first messages need the returned `conversationId` until durable idempotency storage exists.
- Existing databases must run the catalog seed before enabling Agent integration.
- New conversations reuse the first repository-ordered profile; access to existing conversations accepts any profile owned by the user.
- Historical escalation evaluation may use multiple queries; replace it with a focused repository query only after measurement.
- Automated tests do not claim a live Oracle connection.
