using Api.Tests.Verification.Support;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Application.Verification.UseCases;
using Domain.Verification.Enums;
using Infrastructure.Email.Configuration;
using Infrastructure.Security;
using Infrastructure.Verification;
using Infrastructure.Verification.Repositories;
using Infrastructure.Verification.Security;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Api.Tests.Verification;

public sealed class EmailVerificationAcceptanceTests
{
    private readonly InMemoryEmailVerificationSessionRepository _sessionRepo = new();
    private readonly FakeVerificationCodeSender _fakeEmailSender = new();
    private readonly OtpProtector _otpProtector = new(Convert.ToBase64String(new byte[32]));

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EmailVerificationOptions _options = new()
    {
        OtpTtlMinutes = 5,
        OtpMaximumAttempts = 3,
        OtpResendSeconds = 60
    };
    private readonly ConfiguredEmailVerificationSettings _settings;
    private readonly VerificationCodeDispatcher _dispatcher;

    public EmailVerificationAcceptanceTests()
    {
        _settings = new ConfiguredEmailVerificationSettings(Options.Create(_options));
        _dispatcher = new VerificationCodeDispatcher([_fakeEmailSender]);
    }

    [Fact]
    public async Task Acceptance_FullFlow_Email_Confirm_Proof_Success()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));
        var requestHandler = new RequestEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _dispatcher,
            _settings,
            timeProvider,
            _unitOfWork);

        var confirmHandler = new ConfirmEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _settings,
            timeProvider,
            _unitOfWork);

        const string testEmail = "cliente@ejemplo.test";

        // Step 1: Solicitar código OTP por correo.
        var sessionId = await requestHandler.Handle(
            new RequestEmailVerificationCodeCommand(testEmail),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, sessionId);
        Assert.Single(_fakeEmailSender.SentMessages);
        var sentMessage = _fakeEmailSender.SentMessages.Single();
        Assert.Equal(testEmail, sentMessage.Destination);
        Assert.Equal(6, sentMessage.Code.Length);

        // Step 2: Confirmar código OTP correcto y obtener el comprobante (Proof).
        var proof = await confirmHandler.Handle(
            new ConfirmEmailVerificationCodeCommand(sessionId, sentMessage.Code),
            CancellationToken.None);

        Assert.NotNull(proof);
        Assert.Equal(testEmail, proof.Email);
        Assert.NotEmpty(proof.ProofToken);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, proof.VerifiedAtUtc);
    }

    [Fact]
    public async Task Acceptance_InvalidOtp_IncrementsAttempts_And_ExceedingMaxAttempts_BlocksSession()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));
        var requestHandler = new RequestEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _dispatcher,
            _settings,
            timeProvider,
            _unitOfWork);

        var confirmHandler = new ConfirmEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _settings,
            timeProvider,
            _unitOfWork);

        const string testEmail = "intento.fallido@ejemplo.test";

        var sessionId = await requestHandler.Handle(
            new RequestEmailVerificationCodeCommand(testEmail),
            CancellationToken.None);

        // Intentos erróneos 1 y 2.
        for (var i = 1; i < _options.OtpMaximumAttempts; i++)
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                confirmHandler.Handle(
                    new ConfirmEmailVerificationCodeCommand(sessionId, "000000"),
                    CancellationToken.None));
        }

        // Intento erróneo 3 (Alcanza el máximo y bloquea la sesión).
        await Assert.ThrowsAsync<BadRequestException>(() =>
            confirmHandler.Handle(
                new ConfirmEmailVerificationCodeCommand(sessionId, "000000"),
                CancellationToken.None));

        var session = await _sessionRepo.GetByIdAsync(sessionId);
        Assert.NotNull(session);
        Assert.Equal(VerificationSessionStatus.Blocked, session.Status);
    }

    [Fact]
    public async Task Acceptance_ResendTooSoon_ThrowsRateLimitConflict_429()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));
        var requestHandler = new RequestEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _dispatcher,
            _settings,
            timeProvider,
            _unitOfWork);

        const string testEmail = "resend.limit@ejemplo.test";

        // Primer envío exitoso.
        await requestHandler.Handle(
            new RequestEmailVerificationCodeCommand(testEmail),
            CancellationToken.None);

        // Reintento antes de que pase OtpResendSeconds (60 seg) -> debe lanzar ConflictException (429/Conflict).
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            requestHandler.Handle(
                new RequestEmailVerificationCodeCommand(testEmail),
                CancellationToken.None));

        Assert.Contains("El código ya fue enviado", exception.Message);
    }

    [Fact]
    public async Task Acceptance_ExpiredOtp_FailsValidation()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));
        var requestHandler = new RequestEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _dispatcher,
            _settings,
            timeProvider,
            _unitOfWork);

        var confirmHandler = new ConfirmEmailVerificationCodeCommandHandler(
            _sessionRepo,
            _otpProtector,
            _settings,
            timeProvider,
            _unitOfWork);

        const string testEmail = "expirado@ejemplo.test";

        var sessionId = await requestHandler.Handle(
            new RequestEmailVerificationCodeCommand(testEmail),
            CancellationToken.None);

        var sentCode = _fakeEmailSender.SentMessages.Single().Code;

        // Avanzar el tiempo más allá de OtpTtlMinutes (5 minutos).
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            confirmHandler.Handle(
                new ConfirmEmailVerificationCodeCommand(sessionId, sentCode),
                CancellationToken.None));

        Assert.Contains("ha expirado", exception.Message);
    }

    private sealed class MutableTimeProvider(DateTimeOffset initialTime) : TimeProvider
    {
        private DateTimeOffset _currentTime = initialTime;

        public override DateTimeOffset GetUtcNow() => _currentTime;

        public void Advance(TimeSpan delta) => _currentTime = _currentTime.Add(delta);
    }
}
