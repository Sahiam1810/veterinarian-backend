using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Owners.UseCases;
using Application.Roles.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.Verification.Abstractions;
using Domain.Clients.Entities;
using Domain.ContactVerification.Enums;
using Domain.Roles.Entities;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Owners;

// Núcleo Etapa 4: fakes de repos + ConsumeProof. Sin HTTP ni Telegram delete.
public sealed class RegisterOwnerCommandHandlerTests
{
    private const string Email = "ana@huellitas.test";
    private const string EmailHash = "email-hash-ana";
    private const string Phone = "3001234567";
    private const string Identification = "1234567890";
    private static readonly Guid ProofSessionId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private const string Proof = "single-use-proof";

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsersRepository users = Substitute.For<IUsersRepository>();
    private readonly IClientRepository clients = Substitute.For<IClientRepository>();
    private readonly IRolesRepository roles = Substitute.For<IRolesRepository>();
    private readonly IUserAccountsRepository accounts = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository credentials = Substitute.For<IUserCredentialsRepository>();
    private readonly IConsumeContactVerificationProof consumeProof =
        Substitute.For<IConsumeContactVerificationProof>();
    private readonly IOtpProtector otpProtector = Substitute.For<IOtpProtector>();
    private readonly RoleEntity clientRole = new("Cliente", "Dueño");

    public RegisterOwnerCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(users);
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.RolesRepository.Returns(roles);
        unitOfWork.UserAccountsRepository.Returns(accounts);
        unitOfWork.UserCredentialsRepository.Returns(credentials);
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        roles.GetByNameAsync("Cliente", Arg.Any<CancellationToken>()).Returns(clientRole);
        users.ExistsByEmailAsync(Email, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(false);
        clients.ExistsByIdentificationNumberAsync(Identification, Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clients.ExistsByPhoneAsync(Phone, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(false);
        otpProtector.HashEmail(Email).Returns(EmailHash);
    }

    [Fact]
    public async Task Handle_staff_without_proof_flag_creates_client_user_without_password_or_login()
    {
        var sut = CreateSut(requireStaffProof: false);
        UserEntity? persistedUser = null;
        ClientEntity? persistedClient = null;
        users.AddAsync(Arg.Do<UserEntity>(user => persistedUser = user), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        clients.AddAsync(Arg.Do<ClientEntity>(client => persistedClient = client), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await sut.Handle(StaffCommand(), CancellationToken.None);

        Assert.NotNull(persistedUser);
        Assert.Null(persistedUser!.PasswordHash);
        Assert.Equal(clientRole.Id, persistedUser.RoleId);
        Assert.Equal(Email, persistedUser.Email.Value);
        Assert.NotNull(persistedClient);
        Assert.Equal(result.UserId, persistedUser.Id);
        Assert.Equal(result.ClientId, persistedClient!.Id);
        await consumeProof.DidNotReceive().ConsumeAsync(
            Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>());
        await accounts.DidNotReceive().AddAsync(Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
        await credentials.DidNotReceive().AddAsync(Arg.Any<UserCredentialsEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_bot_consumes_register_proof_matching_email()
    {
        var sut = CreateSut(requireStaffProof: false);
        consumeProof.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumedContactVerificationProof(
                ProofSessionId,
                ContactVerificationPurpose.Register,
                SubjectUserId: null,
                EmailHash));

        await sut.Handle(BotCommand(), CancellationToken.None);

        await consumeProof.Received(1).ConsumeAsync(
            Arg.Is<ConsumeContactVerificationProof>(p =>
                p.SessionId == ProofSessionId && p.Proof == Proof),
            Arg.Any<CancellationToken>());
        await users.Received(1).AddAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_bot_without_proof_throws_ProofRequired()
    {
        var sut = CreateSut(requireStaffProof: false);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            sut.Handle(BotCommand() with { ContactProofSessionId = null, ContactProof = null },
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofRequired.Code, error.Code);
        await users.DidNotReceive().AddAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_telegram_always_requires_proof_even_when_staff_flag_is_false()
    {
        var sut = CreateSut(requireStaffProof: false);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            sut.Handle(
                StaffCommand() with { Channel = RegisterOwnerChannel.Telegram },
                CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofRequired.Code, error.Code);
    }

    [Fact]
    public async Task Handle_staff_with_flag_requires_proof()
    {
        var sut = CreateSut(requireStaffProof: true);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            sut.Handle(StaffCommand(), CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofRequired.Code, error.Code);
    }

    [Fact]
    public async Task Handle_rejects_claim_proof_for_new_owner()
    {
        var sut = CreateSut(requireStaffProof: false);
        consumeProof.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumedContactVerificationProof(
                ProofSessionId,
                ContactVerificationPurpose.Claim,
                Guid.NewGuid(),
                EmailHash));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            sut.Handle(BotCommand(), CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofPurposeInvalid.Code, error.Code);
        await users.DidNotReceive().AddAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_proof_bound_to_another_email()
    {
        var sut = CreateSut(requireStaffProof: false);
        consumeProof.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumedContactVerificationProof(
                ProofSessionId,
                ContactVerificationPurpose.Register,
                null,
                "other-hash"));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            sut.Handle(BotCommand(), CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.ProofEmailMismatch.Code, error.Code);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_email_exists()
    {
        users.ExistsByEmailAsync(Email, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(true);
        var sut = CreateSut(requireStaffProof: false);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.Handle(StaffCommand(), CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.EmailAlreadyInUse.Code, error.Code);
        await consumeProof.DidNotReceive().ConsumeAsync(
            Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_conflict_when_identification_exists()
    {
        clients.ExistsByIdentificationNumberAsync(Identification, Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);
        var sut = CreateSut(requireStaffProof: false);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.Handle(StaffCommand(), CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.IdentificationAlreadyInUse.Code, error.Code);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_phone_exists()
    {
        clients.ExistsByPhoneAsync(Phone, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(true);
        var sut = CreateSut(requireStaffProof: false);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.Handle(StaffCommand(), CancellationToken.None));

        Assert.Equal(OwnerRegistrationErrors.PhoneAlreadyInUse.Code, error.Code);
    }

    private RegisterOwnerCommandHandler CreateSut(bool requireStaffProof) =>
        new(unitOfWork, consumeProof, new StubSettings(requireStaffProof), otpProtector);

    private static RegisterOwnerCommand StaffCommand() =>
        new("Ana Dueña", Email, Identification, Phone, RegisterOwnerChannel.Staff, "Calle 1");

    private static RegisterOwnerCommand BotCommand() =>
        StaffCommand() with
        {
            Channel = RegisterOwnerChannel.Bot,
            ContactProofSessionId = ProofSessionId,
            ContactProof = Proof
        };

    private sealed class StubSettings(bool requireContactProofs) : IRegisterOwnerSettings
    {
        public bool RequireContactProofs { get; } = requireContactProofs;

        public bool RequiresContactProof(RegisterOwnerChannel channel) =>
            channel is RegisterOwnerChannel.Bot or RegisterOwnerChannel.Telegram
            || RequireContactProofs;
    }
}
