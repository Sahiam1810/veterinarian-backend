using Application.Clients.Abstraction;
using Application.Common.Abstractions;
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
using Domain.Clients.ValueObjects;
using Domain.ContactVerification.Enums;
using Domain.Roles.Entities;
using NSubstitute;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Owners.Acceptance;

// Doubles in-memory del kickoff: repos + ConsumeProof. Sin SMTP ni HTTP 4.1–4.4.
internal sealed class RegisterOwnerAcceptanceHarness
{
    public const string EmailHashPrefix = "email:";

    public RoleEntity ClientRole { get; } = new("Cliente", "Dueño");
    public InMemoryUsers Users { get; } = new();
    public InMemoryClients Clients { get; } = new();
    public InMemoryAccounts Accounts { get; } = new();
    public InMemoryCredentials Credentials { get; } = new();
    public FakeConsumeProof ConsumeProof { get; } = new();
    public FakeEmailHasher EmailHasher { get; } = new();
    public IUnitOfWork UnitOfWork { get; }
    public InMemoryRoles Roles { get; }

    public RegisterOwnerAcceptanceHarness()
    {
        Roles = new InMemoryRoles(ClientRole);
        UnitOfWork = Substitute.For<IUnitOfWork>();
        UnitOfWork.UsersRepository.Returns(Users);
        UnitOfWork.ClientsRepository.Returns(Clients);
        UnitOfWork.RolesRepository.Returns(Roles);
        UnitOfWork.UserAccountsRepository.Returns(Accounts);
        UnitOfWork.UserCredentialsRepository.Returns(Credentials);
        UnitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
    }

    public RegisterOwnerCommandHandler Handler(bool requireStaffProof) =>
        new(
            UnitOfWork,
            ConsumeProof,
            new FlagSettings(requireStaffProof),
            EmailHasher,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RegisterOwnerCommandHandler>.Instance);

    public IssuedProof IssueRegisterProof(string email)
    {
        var proof = $"proof-{Guid.NewGuid():N}";
        var sessionId = Guid.NewGuid();
        ConsumeProof.Issue(
            sessionId,
            proof,
            ContactVerificationPurpose.Register,
            EmailHasher.HashEmail(email.Trim().ToLowerInvariant()));
        return new IssuedProof(sessionId, proof);
    }

    public void IssueClaimProof(string email, Guid sessionId, string proof)
    {
        ConsumeProof.Issue(
            sessionId,
            proof,
            ContactVerificationPurpose.Claim,
            EmailHasher.HashEmail(email.Trim().ToLowerInvariant()));
    }

    private sealed class FlagSettings(bool requireStaffProof) : IRegisterOwnerSettings
    {
        public bool RequireContactProofs { get; } = requireStaffProof;

        public bool RequiresContactProof(RegisterOwnerChannel channel) =>
            channel is RegisterOwnerChannel.Bot
            || RequireContactProofs;
    }
}

internal sealed record IssuedProof(Guid SessionId, string Proof);

internal sealed class FakeEmailHasher : IOtpProtector
{
    public GeneratedOtp Create() => throw new NotSupportedException("4.5 no pide OTP (Etapa 3).");

    public bool Verify(string code, string expectedHash) => throw new NotSupportedException();

    public string HashEmail(string normalizedEmail) =>
        RegisterOwnerAcceptanceHarness.EmailHashPrefix + normalizedEmail.Trim().ToLowerInvariant();

    public string HashPhone(string normalizedPhone) => throw new NotSupportedException();
}

internal sealed class FakeConsumeProof : IConsumeContactVerificationProof
{
    private readonly Dictionary<Guid, ProofRecord> _issued = [];

    public IReadOnlyCollection<Guid> ConsumedSessionIds { get; private set; } = [];

    public void Issue(
        Guid sessionId,
        string proof,
        ContactVerificationPurpose purpose,
        string destinationHash) =>
        _issued[sessionId] = new ProofRecord(proof, purpose, destinationHash, Consumed: false);

    public Task<ConsumedContactVerificationProof> ConsumeAsync(
        ConsumeContactVerificationProof request,
        CancellationToken cancellationToken)
    {
        if (!_issued.TryGetValue(request.SessionId, out var record)
            || record.Proof != request.Proof)
        {
            throw new ContactVerificationException(ContactVerificationErrors.ProofInvalid);
        }

        if (record.Consumed)
        {
            throw new ContactVerificationException(ContactVerificationErrors.ProofAlreadyConsumed);
        }

        _issued[request.SessionId] = record with { Consumed = true };
        ConsumedSessionIds = [.. ConsumedSessionIds, request.SessionId];
        return Task.FromResult(new ConsumedContactVerificationProof(
            request.SessionId,
            record.Purpose,
            SubjectUserId: null,
            record.DestinationHash));
    }

    private sealed record ProofRecord(
        string Proof,
        ContactVerificationPurpose Purpose,
        string DestinationHash,
        bool Consumed);
}

internal sealed class InMemoryRoles(RoleEntity clientRole) : IRolesRepository
{
    public Task<RoleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<RoleEntity?>(id == clientRole.Id ? clientRole : null);

    public Task<RoleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult<RoleEntity?>(
            string.Equals(name, "Cliente", StringComparison.Ordinal) ? clientRole : null);

    public Task<IReadOnlyCollection<RoleEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> ExistsByNameAsync(
        string name, CancellationToken cancellationToken, Guid? excludedId = null) =>
        throw new NotSupportedException();

    public Task AddAsync(RoleEntity role, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task UpdateAsync(RoleEntity role, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task DeleteAsync(RoleEntity role, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class InMemoryUsers : IUsersRepository
{
    public List<UserEntity> Items { get; } = [];

    public Task AddAsync(UserEntity user, CancellationToken cancellationToken)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }

    public Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(user => user.Id == id));

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Task.FromResult(Items.Any(user =>
            user.Email.Value == normalized && (!excludedId.HasValue || user.Id != excludedId.Value)));
    }

    public Task<IReadOnlyCollection<UserEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task UpdateAsync(UserEntity user, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task DeleteAsync(UserEntity user, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class InMemoryClients : IClientRepository
{
    public List<ClientEntity> Items { get; } = [];

    public Task AddAsync(ClientEntity client, CancellationToken cancellationToken)
    {
        Items.Add(client);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByIdentificationNumberAsync(
        string identificationNumber,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var expected = ClientIdentificationNumber.Create(identificationNumber).Value;
        return Task.FromResult(Items.Any(client =>
            client.IdentificationNumber.Value == expected
            && (!excludedId.HasValue || client.Id != excludedId.Value)));
    }

    public Task<bool> ExistsByPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var expected = ClientPhoneNumber.Create(phoneNumber).Value;
        return Task.FromResult(Items.Any(client =>
            client.PhoneNumber?.Value == expected
            && (!excludedId.HasValue || client.Id != excludedId.Value)));
    }

    public Task<bool> ExistsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken, Guid? excludedId = null) =>
        Task.FromResult(Items.Any(client =>
            client.UserId == userId && (!excludedId.HasValue || client.Id != excludedId.Value)));

    public Task<IReadOnlyCollection<ClientEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ClientEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    // Misma normalización que ClientRepository: lookup Etapa 2 tras RegisterOwner.
    public Task<ClientEntity?> GetByIdentificationNumberAsync(
        string identificationNumber, CancellationToken cancellationToken)
    {
        var expected = ClientIdentificationNumber.Create(identificationNumber).Value;
        return Task.FromResult(Items.FirstOrDefault(client =>
            client.IdentificationNumber.Value == expected));
    }

    public Task<ClientEntity?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var expected = ClientPhoneNumber.Create(phoneNumber).Value;
        return Task.FromResult(Items.FirstOrDefault(client =>
            client.PhoneNumber?.Value == expected));
    }

    public Task<ClientEntity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(client => client.UserId == userId));

    public Task<ClientEntity?> GetByLookupAsync(
        string? identificationNumber, string? phoneNumber, CancellationToken cancellationToken)
    {
        IEnumerable<ClientEntity> query = Items;
        if (!string.IsNullOrWhiteSpace(identificationNumber))
        {
            var idVo = ClientIdentificationNumber.Create(identificationNumber).Value;
            query = query.Where(client => client.IdentificationNumber.Value == idVo);
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneVo = ClientPhoneNumber.Create(phoneNumber).Value;
            query = query.Where(client => client.PhoneNumber?.Value == phoneVo);
        }

        return Task.FromResult(query.FirstOrDefault());
    }

    public Task UpdateAsync(ClientEntity client, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task DeleteAsync(ClientEntity client, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<Guid?> GetIdByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(client => client.UserId == userId)?.Id);

    public Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        Items.RemoveAll(client => client.UserId == userId);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryAccounts : IUserAccountsRepository
{
    public List<UserAccountEntity> Items { get; } = [];

    public Task AddAsync(UserAccountEntity account, CancellationToken cancellationToken)
    {
        Items.Add(account);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken, Guid? excludedId = null) =>
        Task.FromResult(Items.Any(account =>
            account.UserId == userId && (!excludedId.HasValue || account.Id != excludedId.Value)));

    public Task<UserAccountEntity?> GetByMailAsync(string mail, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(account =>
            string.Equals(account.Mail.Value, mail, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyCollection<UserAccountEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UserAccountEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UserAccountEntity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(account => account.UserId == userId));

    public Task<bool> ExistsByUsernameAsync(
        string username, CancellationToken cancellationToken, Guid? excludedId = null) =>
        throw new NotSupportedException();

    public Task<bool> ExistsByMailAsync(
        string mail, CancellationToken cancellationToken, Guid? excludedId = null) =>
        throw new NotSupportedException();

    public Task UpdateAsync(UserAccountEntity account, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task DeleteAsync(UserAccountEntity account, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class InMemoryCredentials : IUserCredentialsRepository
{
    public List<UserCredentialsEntity> Items { get; } = [];

    public Task AddAsync(UserCredentialsEntity credentials, CancellationToken cancellationToken)
    {
        Items.Add(credentials);
        return Task.CompletedTask;
    }

    public Task<UserCredentialsEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UserCredentialsEntity?> GetByAccountIdAsync(
        Guid accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(item => item.AccountId == accountId));

    public Task<bool> ExistsByAccountIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(item => item.AccountId == accountId));

    public Task UpdateAsync(UserCredentialsEntity credentials, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
