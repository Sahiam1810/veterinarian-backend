using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Owners.UseCases;
using Application.Verification.Abstractions;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Domain.ContactVerification.Enums;
using NSubstitute;

namespace Application.Tests.Owners.Acceptance;

// Doubles in-memory: repositorio de clientes + ConsumeProof. El alta de dueño no toca USERS.
internal sealed class RegisterOwnerAcceptanceHarness
{
    public const string EmailHashPrefix = "email:";

    public InMemoryClients Clients { get; } = new();
    public FakeConsumeProof ConsumeProof { get; } = new();
    public FakeEmailHasher EmailHasher { get; } = new();
    public IUnitOfWork UnitOfWork { get; }

    public RegisterOwnerAcceptanceHarness()
    {
        UnitOfWork = Substitute.For<IUnitOfWork>();
        UnitOfWork.ClientsRepository.Returns(Clients);
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

        // Espeja ConfiguredRegisterOwnerSettings: Bot/Telegram nunca exigen proof
        // (decisión de negocio); Staff respeta el flag configurado.
        public bool RequiresContactProof(RegisterOwnerChannel channel) =>
            channel switch
            {
                RegisterOwnerChannel.Bot => false,
                RegisterOwnerChannel.Telegram => false,
                _ => RequireContactProofs
            };
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

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var expected = ClientEmail.Create(email).Value;
        return Task.FromResult(Items.Any(client =>
            client.Email.Value == expected
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

    public Task<ClientEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var expected = ClientEmail.Create(email).Value;
        return Task.FromResult(Items.FirstOrDefault(client => client.Email.Value == expected));
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