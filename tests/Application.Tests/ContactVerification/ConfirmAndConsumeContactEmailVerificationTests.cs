using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.ContactVerification.Security;
using Application.ContactVerification.UseCases;
using Application.Verification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.ContactVerification;

public sealed class ConfirmAndConsumeContactEmailVerificationTests
{
    private const string DestinationHash = "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string KnownCode = "123456";
    private const string KnownOtpHash = "CDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCD";

    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly IContactVerificationSessionRepository sessions =
        Substitute.For<IContactVerificationSessionRepository>();
    private readonly IOtpProtector otpProtector = Substitute.For<IOtpProtector>();
    private readonly IContactVerificationSettings settings = Substitute.For<IContactVerificationSettings>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MutableTimeProvider clock = new(Now);

    private readonly ConfirmContactEmailVerificationHandler confirm;
    private readonly ConsumeContactVerificationProofHandler consume;

    public ConfirmAndConsumeContactEmailVerificationTests()
    {
        settings.OtpMaximumAttempts.Returns(3);
        settings.ProofLifetime.Returns(TimeSpan.FromMinutes(15));
        otpProtector.Verify(KnownCode, KnownOtpHash).Returns(true);

        confirm = new ConfirmContactEmailVerificationHandler(
            sessions, otpProtector, settings, unitOfWork, clock);
        consume = new ConsumeContactVerificationProofHandler(sessions, unitOfWork, clock);
    }

    [Fact]
    public async Task Confirm_valid_otp_issues_proof_clears_otp_hash_and_persists_only_proof_hash()
    {
        var session = StartAwaitingOtp();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await confirm.ConfirmAsync(
            new ConfirmContactEmailVerification(session.Id, KnownCode),
            CancellationToken.None);

        Assert.Equal(session.Id, result.SessionId);
        Assert.False(string.IsNullOrWhiteSpace(result.Proof));
        Assert.Equal(ContactVerificationSessionStatus.ProofIssued, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Equal(ContactVerificationProof.Hash(result.Proof), session.ProofHash);
        Assert.NotEqual(result.Proof, session.ProofHash);
        await sessions.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
        await unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_invalid_otp_increments_attempts_and_returns_InvalidCode()
    {
        var session = StartAwaitingOtp();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        otpProtector.Verify("000000", KnownOtpHash).Returns(false);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(session.Id, "000000"),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.InvalidCode.Code, error.Code);
        Assert.Equal(1, session.Attempts);
        Assert.Equal(ContactVerificationSessionStatus.AwaitingOtp, session.Status);
        Assert.Equal(KnownOtpHash, session.OtpHash);
    }

    [Fact]
    public async Task Confirm_last_invalid_otp_blocks_and_clears_hashes()
    {
        var session = StartAwaitingOtp();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        settings.OtpMaximumAttempts.Returns(1);
        otpProtector.Verify("000000", KnownOtpHash).Returns(false);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(session.Id, "000000"),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.Blocked.Code, error.Code);
        Assert.Equal(ContactVerificationSessionStatus.Blocked, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Null(session.ProofHash);
    }

    [Fact]
    public async Task Confirm_expired_otp_returns_Expired_without_proof()
    {
        var session = StartAwaitingOtp(expiresAt: Now.UtcDateTime.AddMinutes(10));
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        clock.SetUtcNow(Now.AddMinutes(11));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(session.Id, KnownCode),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.Expired.Code, error.Code);
        Assert.Equal(ContactVerificationSessionStatus.Expired, session.Status);
        Assert.Null(session.ProofHash);
        Assert.Null(session.OtpHash);
    }

    [Fact]
    public async Task Confirm_already_proof_issued_returns_SessionNotFound()
    {
        var session = StartAwaitingOtp();
        var proof = ContactVerificationProof.Generate();
        session.IssueProof(
            ContactVerificationProof.Hash(proof),
            Now.UtcDateTime.AddMinutes(15),
            Now.UtcDateTime.AddMinutes(1));
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(session.Id, KnownCode),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.SessionNotFound.Code, error.Code);
    }

    [Fact]
    public async Task Confirm_blocked_session_returns_Blocked()
    {
        var session = StartAwaitingOtp();
        session.RegisterFailedAttempt(1, Now.UtcDateTime.AddSeconds(1));
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(session.Id, KnownCode),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.Blocked.Code, error.Code);
    }

    [Fact]
    public async Task Consume_valid_proof_succeeds_and_marks_consumed()
    {
        var (session, proof) = IssueProofSession();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        sessions.TryConsumeProofAsync(
                session.Id, session.ProofHash!, Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await consume.ConsumeAsync(
            new ConsumeContactVerificationProof(session.Id, proof),
            CancellationToken.None);

        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal(ContactVerificationPurpose.Register, result.Purpose);
        await sessions.Received(1).TryConsumeProofAsync(
            session.Id, ContactVerificationProof.Hash(proof), Now.UtcDateTime, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_second_use_returns_ProofAlreadyConsumed()
    {
        var (session, proof) = IssueProofSession();
        session.ConsumeProof(Now.UtcDateTime.AddMinutes(1));
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            consume.ConsumeAsync(
                new ConsumeContactVerificationProof(session.Id, proof),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofAlreadyConsumed.Code, error.Code);
        await sessions.DidNotReceive().TryConsumeProofAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_expired_proof_returns_ProofExpired()
    {
        var session = StartAwaitingOtp();
        var proof = ContactVerificationProof.Generate();
        session.IssueProof(
            ContactVerificationProof.Hash(proof),
            Now.UtcDateTime.AddMinutes(15),
            Now.UtcDateTime.AddMinutes(1));
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        clock.SetUtcNow(Now.AddMinutes(20));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            consume.ConsumeAsync(
                new ConsumeContactVerificationProof(session.Id, proof),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofExpired.Code, error.Code);
        Assert.Equal(ContactVerificationSessionStatus.Expired, session.Status);
    }

    [Fact]
    public async Task Consume_wrong_proof_returns_ProofInvalid()
    {
        var (session, _) = IssueProofSession();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            consume.ConsumeAsync(
                new ConsumeContactVerificationProof(session.Id, "deadbeef"),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofInvalid.Code, error.Code);
    }

    [Fact]
    public async Task Consume_race_only_one_caller_succeeds()
    {
        var store = new ConcurrentSessionStore();
        var (session, proof) = IssueProofSession();
        store.Add(session);

        var left = new ConsumeContactVerificationProofHandler(store, unitOfWork, clock);
        var right = new ConsumeContactVerificationProofHandler(store, unitOfWork, clock);
        var request = new ConsumeContactVerificationProof(session.Id, proof);

        var results = await Task.WhenAll(
            TryConsumeAsync(left, request),
            TryConsumeAsync(right, request));

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Equal(1, results.Count(result =>
            !result.Succeeded && result.Code == ContactVerificationErrors.ProofAlreadyConsumed.Code));
        Assert.Equal(ContactVerificationSessionStatus.Consumed, store.GetRequired(session.Id).Status);
        Assert.Null(store.GetRequired(session.Id).ProofHash);
    }

    [Fact]
    public async Task Consume_rejects_authorization_with_session_id_only()
    {
        var (session, _) = IssueProofSession();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            consume.ConsumeAsync(
                new ConsumeContactVerificationProof(session.Id, " "),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofInvalid.Code, error.Code);
    }

    private static async Task<(bool Succeeded, string? Code)> TryConsumeAsync(
        ConsumeContactVerificationProofHandler handler,
        ConsumeContactVerificationProof request)
    {
        try
        {
            await handler.ConsumeAsync(request, CancellationToken.None);
            return (true, null);
        }
        catch (ContactVerificationException exception)
        {
            return (false, exception.Code);
        }
    }

    private (ContactVerificationSession Session, string Proof) IssueProofSession()
    {
        var session = StartAwaitingOtp();
        var proof = ContactVerificationProof.Generate();
        session.IssueProof(
            ContactVerificationProof.Hash(proof),
            Now.UtcDateTime.AddMinutes(15),
            Now.UtcDateTime.AddMinutes(1));
        return (session, proof);
    }

    private static ContactVerificationSession StartAwaitingOtp(DateTime? expiresAt = null) =>
        ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            DestinationHash,
            KnownOtpHash,
            expiresAt ?? Now.UtcDateTime.AddMinutes(10),
            Now.UtcDateTime);

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public void SetUtcNow(DateTimeOffset value) => _now = value;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    // Persistencia concurrente in-process (simula TryConsume atómico del repositorio).
    private sealed class ConcurrentSessionStore : IContactVerificationSessionRepository
    {
        private readonly Dictionary<Guid, ContactVerificationSession> _sessions = new();
        private readonly object _gate = new();

        public void Add(ContactVerificationSession session)
        {
            lock (_gate)
            {
                _sessions[session.Id] = session;
            }
        }

        public ContactVerificationSession GetRequired(Guid id)
        {
            lock (_gate)
            {
                return _sessions[id];
            }
        }

        public Task<ContactVerificationSession?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    _sessions.TryGetValue(id, out var session) ? session : null);
            }
        }

        public Task<ContactVerificationSession?> GetActiveByPurposeAndDestinationAsync(
            ContactVerificationPurpose purpose,
            string destinationHash,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ContactVerificationSession?> GetByProofHashAsync(
            string proofHash,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(
            ContactVerificationSession session,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateAsync(
            ContactVerificationSession session,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _sessions[session.Id] = session;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeProofAsync(
            Guid sessionId,
            string proofHash,
            DateTime consumedAt,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    return Task.FromResult(false);
                }

                if (session.Status != ContactVerificationSessionStatus.ProofIssued
                    || session.ProofHash != proofHash
                    || session.ProofExpiresAt is null
                    || consumedAt >= session.ProofExpiresAt)
                {
                    return Task.FromResult(false);
                }

                session.ConsumeProof(consumedAt);
                return Task.FromResult(true);
            }
        }
    }
}
