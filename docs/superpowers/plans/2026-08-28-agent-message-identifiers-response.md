# Agent Message Identifiers and Full Response Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (\`- [ ]\`) syntax for tracking.

**Goal:** Make the agent gateway headers optional with backend-generated identifiers and expose the complete typed FastAPI message response.

**Architecture:** API resolves optional transport identifiers before dispatching the existing command. Application owns provider-neutral response records, Infrastructure maps the complete FastAPI contract into them, and API serializes dedicated public DTOs. Domain and Oracle persistence remain untouched.

**Tech Stack:** .NET 10, ASP.NET Core controllers/OpenAPI, MediatR, System.Text.Json, xUnit, WebApplicationFactory.

## Global Constraints

- Work on the existing \`feature/agent-messaging-gateway\` branch without a worktree.
- Preserve authenticated-fallback authorization and derive identity only from JWT claims.
- Do not add Oracle persistence, migrations, retries, or unrelated refactors.
- Use TDD: each production behavior must first be observed failing in a focused test.
- Use Conventional Commits with the established emoji convention.

## Baseline

- Target module: \`Agent\`.
- Existing endpoint: authenticated \`POST /api/agent/messages\`.
- Git state before this plan: clean feature branch plus committed design documentation.
- Baseline: 107 tests passing (\`Application.Tests\` 17, \`Infrastructure.Tests\` 44, \`Api.Tests\` 40).

## Approved Use Case Contract

- Business objective: callers can omit transport identifiers and receive complete agent metadata.
- Command/query: existing \`SendAgentMessageCommand\` with resolved, non-empty identifiers.
- Inputs: existing body; optional \`Idempotency-Key\`; optional \`X-Correlation-ID\`.
- Output: message identifiers, response type, provider/model, token usage, module and complete RAG metadata.
- Errors: preserve current validation and safe gateway error translations.
- Authorization: \`authenticated-fallback\`; this is an ordinary authenticated business endpoint.
- Reference-controller authorization explicitly excluded from inference: no administrative policy is added.
- Transaction/idempotency/concurrency: no transaction; supplied key is preserved; absent/blank key becomes \`msg-{Guid.NewGuid():N}\`; durable deduplication is out of scope.
- Side effects: one existing HTTP call to FastAPI.
- Compatibility: existing clients that send headers continue to work; request body and route are unchanged.

## Layer impact

| Layer | Required? | Existing files changed | New files | Reason |
|---|---:|---|---|---|
| Domain | no | none | none | No invariant or persisted state changes. |
| Application | yes | \`AgentMessageResult.cs\` and tests using it | \`AgentTokenUsage.cs\`, \`AgentRagResult.cs\` | Own the provider-neutral complete result. |
| Infrastructure | yes | \`AgentHttpResponse.cs\`, \`AgentMessagingHttpClient.cs\`, HTTP tests | none required | Deserialize and map FastAPI metadata. |
| Api | yes | response DTO, controller, HTTP tests | nested DTOs may share the response file | Generate omitted identifiers and expose/document response. |

---

### Task 1: Complete provider-neutral agent result and HTTP mapping

**Files:**
- Create: \`src/Application/Agent/Messages/AgentTokenUsage.cs\`
- Create: \`src/Application/Agent/Messages/AgentRagResult.cs\`
- Modify: \`src/Application/Agent/Messages/AgentMessageResult.cs\`
- Modify: \`src/Infrastructure/Agent/Http/Contracts/AgentHttpResponse.cs\`
- Modify: \`src/Infrastructure/Agent/Http/AgentMessagingHttpClient.cs\`
- Modify: \`tests/Infrastructure.Tests/Agent/Http/AgentMessagingHttpClientTests.cs\`
- Modify: \`tests/Application.Tests/Agent/Messages/SendAgentMessageHandlerTests.cs\`

**Interfaces:**
- Produces: \`AgentTokenUsage(int? InputTokens, int? OutputTokens)\`.
- Produces: \`AgentRagResult(string Status, string Route, double? TopScore, int GlobalMatches, int ConversationMatches, bool MemoryStored, bool KnowledgePublished)\`.
- Produces: \`AgentMessageResult(string? Message, Guid ConversationId, Guid CorrelationId, string ResponseType, string? Provider, string? Model, AgentTokenUsage? Usage, string? Module, AgentRagResult Rag)\`.

- [ ] **Step 1: Write the failing complete-contract HTTP test**

Extend \`SuccessJson()\` with the already representative complete payload and replace the reduced-result assertion with literal checks:

\`\`\`csharp
Assert.Equal("openai", result.Provider);
Assert.Equal("gpt-4o-mini", result.Model);
Assert.Equal(10, result.Usage!.InputTokens);
Assert.Equal(5, result.Usage.OutputTokens);
Assert.Equal("used", result.Rag.Status);
Assert.Equal("contextual", result.Rag.Route);
Assert.Equal(0.91, result.Rag.TopScore);
Assert.Equal(2, result.Rag.GlobalMatches);
Assert.Equal(1, result.Rag.ConversationMatches);
Assert.True(result.Rag.MemoryStored);
Assert.False(result.Rag.KnowledgePublished);
\`\`\`

Use this RAG fixture:

\`\`\`json
"rag":{
  "status":"used",
  "route":"contextual",
  "topScore":0.91,
  "globalMatches":2,
  "conversationMatches":1,
  "memoryStored":true,
  "knowledgePublished":false
}
\`\`\`

- [ ] **Step 2: Run the focused test and observe RED**

\`\`\`powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentMessagingHttpClientTests.Send_posts_expected_fastapi_contract_and_maps_safe_result"
\`\`\`

Expected: compilation fails because the complete result properties do not exist.

- [ ] **Step 3: Implement the minimal Application and Infrastructure contracts**

Add the two neutral records, extend \`AgentMessageResult\`, model nested nullable \`usage\` and required \`rag\` in \`AgentHttpResponse\`, and map every field in \`AgentMessagingHttpClient.SendAsync\`. Keep JSON names explicit with \`JsonPropertyName\` and use nullable numeric token fields and nullable \`TopScore\` exactly as FastAPI defines them.

- [ ] **Step 4: Update existing Application fixtures and add nullable coverage**

Pass complete \`AgentMessageResult\` values to existing handler tests. Add an Infrastructure success fixture where \`provider\`, \`model\`, \`usage\`, \`module\`, and \`rag.topScore\` are null, then assert deserialization succeeds without inventing values.

- [ ] **Step 5: Verify GREEN**

\`\`\`powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent.Messages"
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent.Http"
\`\`\`

Expected: all selected tests pass.

- [ ] **Step 6: Commit**

\`\`\`powershell
git add src/Application/Agent src/Infrastructure/Agent/Http tests/Application.Tests/Agent tests/Infrastructure.Tests/Agent
git commit -m "feat: :sparkles: map complete agent response"
\`\`\`

**Stop gate:** Application and Infrastructure tests must pass before changing API.

---

### Task 2: Generate omitted transport identifiers in API

**Files:**
- Modify: \`src/Api/Agent/Controllers/AgentMessagesController.cs\`
- Modify: \`tests/Api.Tests/Agent/AgentMessagesHttpTests.cs\`

**Interfaces:**
- Consumes: existing non-null \`SendAgentMessageCommand.IdempotencyKey\` and \`.CorrelationId\`.
- Produces: optional HTTP headers with generated fallback values.

- [ ] **Step 1: Replace the obsolete required-header test with failing generation coverage**

Replace \`Post_without_idempotency_header_returns_bad_request\` with:

\`\`\`csharp
[Fact]
public async Task Post_without_transport_headers_generates_identifiers()
{
    using var client = factory.CreateAuthenticatedClient();
    using var request = CreateRequest(includeIdempotencyKey: false);

    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Matches("^msg-[0-9a-f]{32}$", factory.AgentClient.Envelope!.IdempotencyKey);
    Assert.NotEqual(Guid.Empty, factory.AgentClient.Envelope.CorrelationId);
}
\`\`\`

Add a second test that supplies \`message-001\` and the fixed \`CorrelationId\`, then asserts both are preserved in the recorded envelope.

- [ ] **Step 2: Run the focused tests and observe RED**

\`\`\`powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentMessagesHttpTests.Post_without_transport_headers_generates_identifiers"
\`\`\`

Expected: request returns 400 because model binding currently requires \`Idempotency-Key\`.

- [ ] **Step 3: Implement optional header resolution**

Change the action input to \`string? idempotencyKey\` and resolve values before constructing the command:

\`\`\`csharp
var resolvedIdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
    ? $"msg-{Guid.NewGuid():N}"
    : idempotencyKey;
var resolvedCorrelationId = correlationId ?? Guid.NewGuid();
\`\`\`

Pass only the resolved values into \`SendAgentMessageCommand\`; do not make the Application command nullable.

- [ ] **Step 4: Verify GREEN and OpenAPI optionality**

Run all \`AgentMessagesHttpTests\`. Add an OpenAPI assertion against the generated document proving neither header has \`required: true\`.

- [ ] **Step 5: Commit**

\`\`\`powershell
git add src/Api/Agent/Controllers/AgentMessagesController.cs tests/Api.Tests/Agent/AgentMessagesHttpTests.cs
git commit -m "feat: :sparkles: generate agent request identifiers"
\`\`\`

**Stop gate:** API generation, preservation, authorization, validation and error tests must pass before response DTO changes.

---

### Task 3: Expose the complete response through API and Swagger

**Files:**
- Modify: \`src/Api/Agent/Dtos/SendAgentMessageResponse.cs\`
- Modify: \`src/Api/Agent/Controllers/AgentMessagesController.cs\`
- Modify: \`tests/Api.Tests/Agent/AgentMessagesHttpTests.cs\`

**Interfaces:**
- Consumes: complete \`AgentMessageResult\` from Task 1.
- Produces: \`SendAgentTokenUsageResponse\`, \`SendAgentRagResponse\`, and expanded \`SendAgentMessageResponse\` serialized in camelCase.

- [ ] **Step 1: Write the failing API response test**

Configure \`RecordingAgentMessagingClient\` to return a complete literal result and change the first endpoint test to assert:

\`\`\`csharp
Assert.Equal("openrouter", body.GetProperty("provider").GetString());
Assert.Equal("google/gemini-flash", body.GetProperty("model").GetString());
Assert.Equal(12, body.GetProperty("usage").GetProperty("inputTokens").GetInt32());
Assert.Equal(7, body.GetProperty("usage").GetProperty("outputTokens").GetInt32());
Assert.Equal("direct", body.GetProperty("rag").GetProperty("route").GetString());
Assert.Equal(-1, body.GetProperty("rag").GetProperty("topScore").GetDouble());
Assert.True(body.GetProperty("rag").GetProperty("memoryStored").GetBoolean());
\`\`\`

- [ ] **Step 2: Run the focused test and observe RED**

\`\`\`powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentMessagesHttpTests.Post_derives_identity"
\`\`\`

Expected: response lacks \`provider\`, \`model\`, \`usage\`, or \`rag\`.

- [ ] **Step 3: Implement dedicated public nested DTOs and mapping**

Expand the response record without returning Infrastructure types. Map \`result.Usage\` conditionally and map every required RAG property. Preserve \`null\` rather than constructing fake token usage.

- [ ] **Step 4: Add nullable serialization and OpenAPI schema tests**

Add a response case with nullable provider/model/usage/module/topScore and assert successful JSON with explicit nulls. Assert the OpenAPI success schema exposes \`provider\`, \`model\`, \`usage\`, \`module\`, and \`rag\` and that the nested RAG schema exposes all seven approved fields.

- [ ] **Step 5: Verify GREEN**

\`\`\`powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent"
\`\`\`

Expected: all selected Agent API tests pass.

- [ ] **Step 6: Commit**

\`\`\`powershell
git add src/Api/Agent tests/Api.Tests/Agent
git commit -m "feat: :sparkles: expose complete agent response"
\`\`\`

**Stop gate:** full Agent HTTP contract and OpenAPI checks must be green before final verification.

---

### Task 4: Full verification and documentation alignment

**Files:**
- Modify only if required by observed contract drift: \`README.md\`

- [ ] **Step 1: Run full tests and build**

\`\`\`powershell
dotnet test veterinarian_backend.slnx --configuration Debug --no-restore
dotnet build veterinarian_backend.slnx --configuration Release --no-restore
\`\`\`

Expected: all tests pass; build has zero errors and zero new warnings.

- [ ] **Step 2: Verify formatting and diff safety**

\`\`\`powershell
dotnet format veterinarian_backend.slnx --verify-no-changes --no-restore --include src/Application/Agent src/Infrastructure/Agent src/Api/Agent tests/Application.Tests/Agent tests/Infrastructure.Tests/Agent tests/Api.Tests/Agent
git diff --check
git status --short
\`\`\`

Expected: scoped formatting and diff checks pass; only planned files differ from the branch base.

- [ ] **Step 3: Audit authorization and architecture**

Confirm \`AgentMessagesController\` remains \`[Authorize]\`, has no \`AuthManagementGrant\`, Application has no Infrastructure/API reference, and no Oracle/DbContext/repository/migration was added under Agent.

- [ ] **Step 4: Update README only if it still says \`Idempotency-Key\` is required**

Document both headers as optional, their generated formats, and the limitation that backend-generated keys do not deduplicate separate retries without persistence.

- [ ] **Step 5: Commit documentation if changed**

\`\`\`powershell
git add README.md
git commit -m "docs: :memo: document generated agent identifiers"
\`\`\`

## Final verification

- Full build/tests: solution Debug tests and Release build.
- Endpoint/OpenAPI: optional headers and complete response schemas.
- Authorization/error/idempotency: JWT-derived identity and current safe errors unchanged; supplied and generated identifiers covered.
- \`AuthManagementGrant\`: must remain absent from Agent and must not be default/fallback/global.
- Regression: all existing solution tests compared with the 107-test baseline.
- Remaining risk: generated idempotency keys identify individual backend calls but cannot deduplicate independent client retries until durable message persistence exists.

