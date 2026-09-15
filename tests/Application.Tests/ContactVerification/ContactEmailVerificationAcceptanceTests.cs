using System.Security.Cryptography;
using System.Text;
using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.ContactVerification.UseCases;
using Application.Verification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.ContactVerification;

// Suite aceptación Etapa 3.4: Email → confirm → proof (+ resend), solo fakes. Sin Gmail real ni RegisterOwner.
// Ver docs/smoke/etapa-3-contact-email-exit-gate.md
public sealed class ContactEmailVerificationAcceptanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryContactVerificationSessionStore sessions = new();
    private readonly FakeContactEmailMailbox mailbox = new();
    private readonly FakeOtpProtector otpProtector = new();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MutableTimeProvider clock = new(Now);
    private readonly TestContactVerificationSettings settings = new(
        otpLifetime: TimeSpan.FromMinutes(5),
        otpMaximumAttempts: 3,
        otpResendInterval: TimeSpan.FromSeconds(60),
        proofLifetime: TimeSpan.FromMinutes(15));

    private readonly FakeRequestContactEmailVerification request;
    private readonly ConfirmContactEmailVerificationHandler confirm;
    private readonly ConsumeContactVerificationProofHandler consume;

    public ContactEmailVerificationAcceptanceTests()
    {
        request = new FakeRequestContactEmailVerification(
            sessions, otpProtector, settings, mailbox, clock, unitOfWork);
        confirm = new ConfirmContactEmailVerificationHandler(
            sessions, otpProtector, settings, unitOfWork, clock);
        consume = new ConsumeContactVerificationProofHandler(sessions, unitOfWork, clock);
    }

    [Fact]
    public async Task Acceptance_Email_Confirm_Proof_Consume_Success()
    {
        var requested = await request.RequestAsync(
            new RequestContactEmailVerification("cliente@ejemplo.test", ContactVerificationPurpose.Register),
            CancellationToken.None);

        Assert.Single(mailbox.Sent);
        Assert.Equal("cliente@ejemplo.test", mailbox.Sent[0].Email);
        Assert.Equal(6, mailbox.Sent[0].Code.Length);

        var confirmed = await confirm.ConfirmAsync(
            new ConfirmContactEmailVerification(requested.SessionId, mailbox.Sent[0].Code),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(confirmed.Proof));
        Assert.Equal(requested.SessionId, confirmed.SessionId);

        var consumed = await consume.ConsumeAsync(
            new ConsumeContactVerificationProof(confirmed.SessionId, confirmed.Proof),
            CancellationToken.None);

        Assert.Equal(ContactVerificationPurpose.Register, consumed.Purpose);
        Assert.Equal(ContactVerificationSessionStatus.Consumed, sessions.GetRequired(requested.SessionId).Status);
    }

    [Fact]
    public async Task Acceptance_InvalidOtp_Then_Blocked_UsesCatalogCodes()
    {
        var requested = await request.RequestAsync(
            new RequestContactEmailVerification("bloqueo@ejemplo.test", ContactVerificationPurpose.Register),
            CancellationToken.None);

        for (var i = 0; i < settings.OtpMaximumAttempts - 1; i++)
        {
            var invalid = await Assert.ThrowsAsync<ContactVerificationException>(() =>
                confirm.ConfirmAsync(
                    new ConfirmContactEmailVerification(requested.SessionId, "000000"),
                    CancellationToken.None));
            Assert.Equal(ContactVerificationErrors.InvalidCode.Code, invalid.Code);
        }

        var blocked = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(requested.SessionId, "000000"),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.Blocked.Code, blocked.Code);
        Assert.Equal(ContactVerificationSessionStatus.Blocked, sessions.GetRequired(requested.SessionId).Status);
    }

    [Fact]
    public async Task Acceptance_ResendTooSoon_ReturnsCatalogCode()
    {
        var first = new RequestContactEmailVerification("resend@ejemplo.test", ContactVerificationPurpose.Register);
        await request.RequestAsync(first, CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(30));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            request.RequestAsync(first, CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ResendTooSoon.Code, error.Code);
        Assert.Single(mailbox.Sent);
    }

    [Fact]
    public async Task Acceptance_ExpiredOtp_ReturnsExpiredCode()
    {
        var requested = await request.RequestAsync(
            new RequestContactEmailVerification("expira@ejemplo.test", ContactVerificationPurpose.Register),
            CancellationToken.None);

        var code = mailbox.Sent.Single().Code;
        clock.Advance(TimeSpan.FromMinutes(6));

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            confirm.ConfirmAsync(
                new ConfirmContactEmailVerification(requested.SessionId, code),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.Expired.Code, error.Code);
    }

    [Fact]
    public async Task Acceptance_SecondProofConsume_ReturnsAlreadyConsumed()
    {
        var requested = await request.RequestAsync(
            new RequestContactEmailVerification("proof@ejemplo.test", ContactVerificationPurpose.Register),
            CancellationToken.None);

        var confirmed = await confirm.ConfirmAsync(
            new ConfirmContactEmailVerification(requested.SessionId, mailbox.Sent.Single().Code),
            CancellationToken.None);

        await consume.ConsumeAsync(
            new ConsumeContactVerificationProof(confirmed.SessionId, confirmed.Proof),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            consume.ConsumeAsync(
                new ConsumeContactVerificationProof(confirmed.SessionId, confirmed.Proof),
                CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.ProofAlreadyConsumed.Code, error.Code);
    }

    // Fake 3.1: crea sesión con OTP hasheado y captura el código (sin SMTP/Gmail).
    private sealed class FakeRequestContactEmailVerification(
        InMemoryContactVerificationSessionStore sessions,
        IOtpProtector otpProtector,
        IContactVerificationSettings settings,
        FakeContactEmailMailbox mailbox,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork) : IRequestContactEmailVerification
    {
        public async Task<RequestContactEmailVerificationResult> RequestAsync(
            RequestContactEmailVerification request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var destinationHash = otpProtector.HashEmail(email);
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var active = await sessions.GetActiveByPurposeAndDestinationAsync(
                request.Purpose,
                destinationHash,
                cancellationToken);

            if (active is not null)
            {
                var lastTouch = active.UpdatedAt ?? active.CreatedAt;
                if (now < lastTouch.Add(settings.OtpResendInterval))
                {
                    throw new ContactVerificationException(ContactVerificationErrors.ResendTooSoon);
                }

                active.Cancel(now);
                await sessions.UpdateAsync(active, cancellationToken);
            }

            var otp = otpProtector.Create();
            mailbox.Sent.Add(new SentContactEmail(email, otp.Code));

            var session = ContactVerificationSession.Start(
                request.Purpose,
                ContactVerificationChannel.Email,
                destinationHash,
                otp.Hash,
                now.Add(settings.OtpLifetime),
                now,
                request.SubjectUserId);

            await sessions.AddAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new RequestContactEmailVerificationResult(
                session.Id,
                now.Add(settings.OtpLifetime),
                ContactVerificationChannel.Email);
        }
    }

    // OTP determinista para tests (hash SHA-256 hex de 64 chars).
    private sealed class FakeOtpProtector : IOtpProtector
    {
        private int _sequence;

        public GeneratedOtp Create()
        {
            var code = (100000 + Interlocked.Increment(ref _sequence)).ToString();
            return new GeneratedOtp(code, HashValue(code));
        }

        public bool Verify(string code, string expectedHash) =>
            string.Equals(HashValue(code), expectedHash, StringComparison.OrdinalIgnoreCase);

        public string HashEmail(string normalizedEmail) => HashValue($"email:{normalizedEmail}");

        public string HashPhone(string normalizedPhone) => HashValue($"phone:{normalizedPhone}");

        private static string HashValue(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private sealed class FakeContactEmailMailbox
    {
        public List<SentContactEmail> Sent { get; } = [];
    }

    private sealed record SentContactEmail(string Email, string Code);

    private sealed class TestContactVerificationSettings(
        TimeSpan otpLifetime,
        int otpMaximumAttempts,
        TimeSpan otpResendInterval,
        TimeSpan proofLifetime) : IContactVerificationSettings
    {
        public TimeSpan OtpLifetime { get; } = otpLifetime;
        public int OtpMaximumAttempts { get; } = otpMaximumAttempts;
        public TimeSpan OtpResendInterval { get; } = otpResendInterval;
        public TimeSpan ProofLifetime { get; } = proofLifetime;
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class InMemoryContactVerificationSessionStore : IContactVerificationSessionRepository
    {
        private readonly Dictionary<Guid, ContactVerificationSession> _sessions = new();
        private readonly object _gate = new();

        public ContactVerificationSession GetRequired(Guid id)
        {
            lock (_gate)
            {
                return _sessions[id];
            }
        }

        public Task<ContactVerificationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_sessions.TryGetValue(id, out var session) ? session : null);
            }
        }

        public Task<ContactVerificationSession?> GetActiveByPurposeAndDestinationAsync(
            ContactVerificationPurpose purpose,
            string destinationHash,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var match = _sessions.Values.FirstOrDefault(session =>
                    session.Purpose == purpose
                    && session.DestinationHash == destinationHash
                    && session.IsAlive);
                return Task.FromResult(match);
            }
        }

        public Task<ContactVerificationSession?> GetByProofHashAsync(
            string proofHash,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var match = _sessions.Values.FirstOrDefault(session => session.ProofHash == proofHash);
                return Task.FromResult(match);
            }
        }

        public Task AddAsync(ContactVerificationSession session, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _sessions[session.Id] = session;
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(ContactVerificationSession session, CancellationToken cancellationToken)
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
