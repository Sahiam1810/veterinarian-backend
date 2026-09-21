using Application.Clients.Errors;
using Application.Clients.UseCases;
using Application.Common.Exceptions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Adapters;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Owners.UseCases;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Owners.Acceptance;

// Matriz del núcleo RegisterOwner contra dobles in-memory. El alta de dueño inserta
// solo en CLIENTS: no crea usuario, cuenta ni credenciales.
public sealed class RegisterOwnerStage4AcceptanceTests
{
    private const string FullName = "Ana Dueña";
    private const string Email = "ana.owner@huellitas.test";
    private const string Identification = "1234567890";
    private const string Phone = "3001234567";

    [Fact]
    public async Task Acceptance_BotAdapter_CreatesOnlyTheClient_WithoutAnyUser()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));

        var result = await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(FullName, Email, Identification, Phone, "Calle 1"),
            CancellationToken.None);

        var client = Assert.Single(harness.Clients.Items);
        Assert.Equal(result.ClientId, client.Id);
        Assert.Null(client.UserId);
        Assert.Equal(FullName, client.FullName.Value);
        Assert.Equal(Email, client.Email.Value);
        Assert.True(client.IsActive);
        Assert.Empty(harness.ConsumeProof.ConsumedSessionIds);
    }

    // Tras el alta por bot, cédula y teléfono resuelven por los mismos queries de lookup.
    [Fact]
    public async Task Acceptance_BotAdapter_RegisteredOwner_IsFindableByPhoneAndIdentification()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));

        var result = await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var byPhone = await new GetClientByPhoneQueryHandler(harness.UnitOfWork).Handle(
            new GetClientByPhoneQuery(Phone),
            CancellationToken.None);
        var byLookup = await new GetClientLookupQueryHandler(
            harness.Clients,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetClientLookupQueryHandler>.Instance).Handle(
            new GetClientLookupQuery(Identification, Phone),
            CancellationToken.None);

        Assert.Equal(result.ClientId, byPhone.Id);
        Assert.Equal(result.ClientId, byLookup.Id);
    }

    [Fact]
    public async Task Acceptance_TelegramAdapter_CreatesOnlyTheClient_WithoutContactProof()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = new RegisterOwnerFromTelegram(ForwardingSender(harness, requireStaffProof: false));

        var result = await adapter.RegisterAsync(
            new RegisterOwnerFromTelegramRequest(
                FullName, Email, Identification, Phone, TelegramUserId: 42),
            CancellationToken.None);

        var client = Assert.Single(harness.Clients.Items);
        Assert.Equal(result.ClientId, client.Id);
        Assert.Null(client.UserId);
        Assert.Empty(harness.ConsumeProof.ConsumedSessionIds);
    }

    [Fact]
    public async Task Acceptance_DuplicateEmail_ReturnsClientsEmailAlreadyInUse()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));
        await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromBotRequest(
                    "Otra", Email.ToUpperInvariant(), "9999999999", "3009999999"),
                CancellationToken.None));

        Assert.Equal(ClientErrorCodes.EmailAlreadyInUse, error.Code);
        Assert.Single(harness.Clients.Items);
    }

    [Fact]
    public async Task Acceptance_DuplicateIdentification_ReturnsClientsIdentificationAlreadyInUse()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));
        await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromBotRequest(
                    "Otra", "otra@huellitas.test", Identification, "3009999999"),
                CancellationToken.None));

        Assert.Equal(ClientErrorCodes.IdentificationAlreadyInUse, error.Code);
        Assert.Single(harness.Clients.Items);
    }

    [Fact]
    public async Task Acceptance_DuplicatePhone_ReturnsClientsPhoneAlreadyInUse()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));
        await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromBotRequest(
                    "Otra", "otra@huellitas.test", "9999999999", Phone),
                CancellationToken.None));

        Assert.Equal(ClientErrorCodes.PhoneAlreadyInUse, error.Code);
        Assert.Single(harness.Clients.Items);
    }

    // El canal Staff conserva la regla de proof cuando RequireContactProofs=true
    // (el personal hoy crea clientes con POST /api/clients, sin proof).
    [Fact]
    public async Task Acceptance_StaffChannel_WithProofFlag_ConsumesEmailProofAndCreatesClient()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);

        var result = await harness.Handler(requireStaffProof: true).Handle(
            StaffCommand(issued.SessionId, issued.Proof),
            CancellationToken.None);

        Assert.Equal(issued.SessionId, Assert.Single(harness.ConsumeProof.ConsumedSessionIds));
        Assert.Equal(result.ClientId, Assert.Single(harness.Clients.Items).Id);
    }

    [Fact]
    public async Task Acceptance_StaffChannel_WithProofFlag_WithoutProof_IsRejected()
    {
        var harness = new RegisterOwnerAcceptanceHarness();

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            harness.Handler(requireStaffProof: true).Handle(
                StaffCommand(null, null),
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofRequired.Code, error.Code);
        Assert.Empty(harness.Clients.Items);
    }

    [Fact]
    public async Task Acceptance_ClaimProof_DoesNotRegisterOwner()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var sessionId = Guid.NewGuid();
        const string proof = "claim-proof";
        harness.IssueClaimProof(Email, sessionId, proof);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            harness.Handler(requireStaffProof: true).Handle(
                StaffCommand(sessionId, proof),
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofPurposeInvalid.Code, error.Code);
        Assert.Empty(harness.Clients.Items);
    }

    [Fact]
    public async Task Acceptance_ConsumedProof_CannotRegisterAgain()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);
        var handler = harness.Handler(requireStaffProof: true);
        await handler.Handle(StaffCommand(issued.SessionId, issued.Proof), CancellationToken.None);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            handler.Handle(
                new RegisterOwnerCommand(
                    "Otra", "otra@huellitas.test", "9999999999", "3009999999", RegisterOwnerChannel.Staff,
                    ContactProofSessionId: issued.SessionId, ContactProof: issued.Proof),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofAlreadyConsumed.Code, error.Code);
        Assert.Single(harness.Clients.Items);
    }

    private static RegisterOwnerCommand StaffCommand(Guid? sessionId, string? proof) =>
        new(
            FullName, Email, Identification, Phone, RegisterOwnerChannel.Staff,
            ContactProofSessionId: sessionId, ContactProof: proof);

    private static ISender ForwardingSender(
        RegisterOwnerAcceptanceHarness harness,
        bool requireStaffProof)
    {
        var sender = Substitute.For<ISender>();
        var handler = harness.Handler(requireStaffProof);
        sender.Send(Arg.Any<RegisterOwnerCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => handler.Handle(
                call.Arg<RegisterOwnerCommand>(),
                call.Arg<CancellationToken>()));
        return sender;
    }
}
