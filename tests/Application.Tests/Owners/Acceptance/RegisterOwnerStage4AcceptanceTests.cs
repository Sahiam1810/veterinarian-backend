using Application.Clients.UseCases;
using Application.Common.Exceptions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Adapters;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Owners.UseCases;
using Application.Security.Errors;
using Application.UserAccounts.UseCase;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Owners.Acceptance;

// Tarea 4.5: matriz del núcleo contra interfaces del kickoff. Sin Gmail/Twilio/HTTP 4.1–4.4.
// Docs: docs/smoke/etapa-4-register-owner-exit-gate.md
public sealed class RegisterOwnerStage4AcceptanceTests
{
    private const string FullName = "Ana Dueña";
    private const string Email = "ana.owner@huellitas.test";
    private const string Identification = "1234567890";
    private const string Phone = "3001234567";

    [Fact]
    public async Task Acceptance_Staff_WithoutProofFlag_CreatesClienteWithoutPasswordOrLogin()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = StaffAdapter(harness, requireStaffProof: false);

        var result = await adapter.RegisterAsync(
            new RegisterOwnerFromStaffRequest(FullName, Email, Identification, Phone, "Calle 1"),
            CancellationToken.None);

        var user = Assert.Single(harness.Users.Items);
        var client = Assert.Single(harness.Clients.Items);
        Assert.Equal(result.UserId, user.Id);
        Assert.Equal(result.ClientId, client.Id);
        Assert.Null(user.PasswordHash);
        Assert.Equal("Cliente", harness.ClientRole.Name.Value);
        Assert.Equal(harness.ClientRole.Id, user.RoleId);
        Assert.Empty(harness.Accounts.Items);
        Assert.Empty(harness.Credentials.Items);
        Assert.Empty(harness.ConsumeProof.ConsumedSessionIds);
    }

    [Fact]
    public async Task Acceptance_Staff_WithProofFlag_ConsumesEmailProofAndDoesNotCreateLogin()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);
        var adapter = StaffAdapter(harness, requireStaffProof: true);

        await adapter.RegisterAsync(
            new RegisterOwnerFromStaffRequest(
                FullName, Email, Identification, Phone, "Calle 1", issued.SessionId, issued.Proof),
            CancellationToken.None);

        Assert.Equal(issued.SessionId, Assert.Single(harness.ConsumeProof.ConsumedSessionIds));
        Assert.Null(Assert.Single(harness.Users.Items).PasswordHash);
        Assert.Empty(harness.Accounts.Items);
        Assert.Empty(harness.Credentials.Items);
    }

    [Fact]
    public async Task Acceptance_BotAdapter_AlwaysConsumesRegisterProof()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));

        await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(
                FullName, Email, Identification, Phone, issued.SessionId, issued.Proof),
            CancellationToken.None);

        Assert.Equal(issued.SessionId, Assert.Single(harness.ConsumeProof.ConsumedSessionIds));
        Assert.Null(Assert.Single(harness.Users.Items).PasswordHash);
        Assert.Empty(harness.Accounts.Items);
    }

    // Tras alta bot, cédula/teléfono deben resolver por los mismos queries de lookup Etapa 2.
    [Fact]
    public async Task Acceptance_BotAdapter_RegisteredOwner_IsFindableByPhoneAndIdentification()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));

        var result = await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(
                FullName, Email, Identification, Phone, issued.SessionId, issued.Proof),
            CancellationToken.None);

        var byPhone = await new GetClientByPhoneQueryHandler(harness.UnitOfWork).Handle(
            new GetClientByPhoneQuery(Phone),
            CancellationToken.None);
        var byLookup = await new GetClientLookupQueryHandler(harness.Clients).Handle(
            new GetClientLookupQuery(Identification, Phone),
            CancellationToken.None);

        Assert.Equal(result.ClientId, byPhone.Id);
        Assert.Equal(result.ClientId, byLookup.Id);
        Assert.Equal(result.UserId, byPhone.UserId);
        Assert.Equal(result.UserId, byLookup.UserId);
    }

    [Fact]
    public async Task Acceptance_TelegramAdapter_AlwaysConsumesRegisterProof()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);
        var adapter = new RegisterOwnerFromTelegram(ForwardingSender(harness, requireStaffProof: false));

        await adapter.RegisterAsync(
            new RegisterOwnerFromTelegramRequest(
                FullName, Email, Identification, Phone, issued.SessionId, issued.Proof, TelegramUserId: 42),
            CancellationToken.None);

        Assert.Equal(issued.SessionId, Assert.Single(harness.ConsumeProof.ConsumedSessionIds));
        Assert.Empty(harness.Accounts.Items);
        Assert.Empty(harness.Credentials.Items);
    }

    [Fact]
    public async Task Acceptance_CreateUserAccount_ForRegisteredCliente_IsPlatformAccessDenied()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var registered = await StaffAdapter(harness, requireStaffProof: false).RegisterAsync(
            new RegisterOwnerFromStaffRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ForbiddenException>(() =>
            new CreateUserAccountCommandHandler(harness.UnitOfWork).Handle(
                new CreateUserAccountCommand(
                    registered.UserId, "ana.owner", Email, "Activo"),
                CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, error.Code);
        Assert.Empty(harness.Accounts.Items);
    }

    [Fact]
    public async Task Acceptance_DuplicateEmail_IsConflict()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = StaffAdapter(harness, requireStaffProof: false);
        await adapter.RegisterAsync(
            new RegisterOwnerFromStaffRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromStaffRequest(FullName, Email, "9999999999", "3009999999"),
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.EmailAlreadyInUse.Code, error.Code);
        Assert.Single(harness.Users.Items);
    }

    [Fact]
    public async Task Acceptance_DuplicateIdentification_IsConflict()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = StaffAdapter(harness, requireStaffProof: false);
        await adapter.RegisterAsync(
            new RegisterOwnerFromStaffRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromStaffRequest(
                    FullName, "otra@huellitas.test", Identification, "3009999999"),
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.IdentificationAlreadyInUse.Code, error.Code);
    }

    [Fact]
    public async Task Acceptance_DuplicatePhone_IsConflict()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var adapter = StaffAdapter(harness, requireStaffProof: false);
        await adapter.RegisterAsync(
            new RegisterOwnerFromStaffRequest(FullName, Email, Identification, Phone),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromStaffRequest(
                    FullName, "otra@huellitas.test", "9999999999", Phone),
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.PhoneAlreadyInUse.Code, error.Code);
    }

    [Fact]
    public async Task Acceptance_ClaimProof_DoesNotRegisterOwner()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var sessionId = Guid.NewGuid();
        const string proof = "claim-proof";
        harness.IssueClaimProof(Email, sessionId, proof);
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromBotRequest(
                    FullName, Email, Identification, Phone, sessionId, proof),
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofPurposeInvalid.Code, error.Code);
        Assert.Empty(harness.Users.Items);
    }

    [Fact]
    public async Task Acceptance_ConsumedProof_CannotRegisterAgain()
    {
        var harness = new RegisterOwnerAcceptanceHarness();
        var issued = harness.IssueRegisterProof(Email);
        var adapter = new RegisterOwnerFromBot(ForwardingSender(harness, requireStaffProof: false));
        await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(
                FullName, Email, Identification, Phone, issued.SessionId, issued.Proof),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            adapter.RegisterAsync(
                new RegisterOwnerFromBotRequest(
                    "Otra", "otra@huellitas.test", "9999999999", "3009999999",
                    issued.SessionId, issued.Proof),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofAlreadyConsumed.Code, error.Code);
        Assert.Single(harness.Users.Items);
    }

    private static IRegisterOwnerFromStaff StaffAdapter(
        RegisterOwnerAcceptanceHarness harness,
        bool requireStaffProof) =>
        new RegisterOwnerFromStaff(ForwardingSender(harness, requireStaffProof));

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
