using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.UseCases;
using Application.Verification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Domain.Verification.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Application.Tests.ContactVerification;

public sealed class ContactEmailVerificationRequestHandlerTests
{
    private const string Email = "owner@huellitas.test";
    private const string NormalizedEmail = "owner@huellitas.test";
    private const string DestinationHash = "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string GeneratedCode = "654321";
    private const string GeneratedOtpHash = "EFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEF";

    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IContactVerificationSessionRepository sessions =
        Substitute.For<IContactVerificationSessionRepository>();
    private readonly IOtpProtector otpProtector = Substitute.For<IOtpProtector>();
    private readonly IVerificationCodeDispatcher codeDispatcher = Substitute.For<IVerificationCodeDispatcher>();
    private readonly IContactVerificationSettings settings = Substitute.For<IContactVerificationSettings>();

    private readonly ContactEmailVerificationRequestHandler sut;

    public ContactEmailVerificationRequestHandlerTests()
    {
        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(10));
        settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        settings.OtpMaximumAttempts.Returns(5);

        otpProtector.Create().Returns(new GeneratedOtp(GeneratedCode, GeneratedOtpHash));
        otpProtector.HashEmail(Arg.Any<string>()).Returns(DestinationHash);

        sut = new ContactEmailVerificationRequestHandler(
            unitOfWork, sessions, otpProtector, codeDispatcher, settings, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task RequestAsync_creates_session_hashes_otp_and_dispatches_the_code()
    {
        sessions.GetActiveByPurposeAndDestinationAsync(
                ContactVerificationPurpose.Register, DestinationHash, Arg.Any<CancellationToken>())
            .Returns((ContactVerificationSession?)null);

        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register);

        var result = await sut.RequestAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Equal(Now.AddMinutes(10).UtcDateTime, result.ExpiresAt);
        Assert.Equal(ContactVerificationChannel.Email, result.Channel);

        await codeDispatcher.Received(1).SendAsync(
            VerificationDeliveryChannel.Email, NormalizedEmail, GeneratedCode, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await sessions.Received(1).AddAsync(
            Arg.Is<ContactVerificationSession>(s =>
                s.Purpose == ContactVerificationPurpose.Register
                && s.Channel == ContactVerificationChannel.Email
                && s.DestinationHash == DestinationHash
                && s.OtpHash == GeneratedOtpHash
                && s.Status == ContactVerificationSessionStatus.AwaitingOtp),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_throws_BadRequestException_when_the_email_is_invalid()
    {
        var request = new RequestContactEmailVerification("not-an-email", ContactVerificationPurpose.Register);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.RequestAsync(request, CancellationToken.None));

        otpProtector.DidNotReceive().Create();
        await sessions.DidNotReceive().AddAsync(Arg.Any<ContactVerificationSession>(), Arg.Any<CancellationToken>());
        await codeDispatcher.DidNotReceive().SendAsync(
            Arg.Any<VerificationDeliveryChannel>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_dispatches_the_code_exactly_once()
    {
        sessions.GetActiveByPurposeAndDestinationAsync(
                ContactVerificationPurpose.Register, DestinationHash, Arg.Any<CancellationToken>())
            .Returns((ContactVerificationSession?)null);

        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register);

        await sut.RequestAsync(request, CancellationToken.None);

        await codeDispatcher.Received(1).SendAsync(
            Arg.Any<VerificationDeliveryChannel>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_response_and_persisted_session_never_expose_the_plaintext_otp()
    {
        sessions.GetActiveByPurposeAndDestinationAsync(
                ContactVerificationPurpose.Register, DestinationHash, Arg.Any<CancellationToken>())
            .Returns((ContactVerificationSession?)null);

        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register);

        var result = await sut.RequestAsync(request, CancellationToken.None);

        // RequestContactEmailVerificationResult solo expone SessionId/ExpiresAt/Channel: no hay forma de filtrar el OTP.
        Assert.Equal(3, typeof(RequestContactEmailVerificationResult).GetProperties().Length);
        await sessions.Received(1).AddAsync(
            Arg.Is<ContactVerificationSession>(s => s.OtpHash == GeneratedOtpHash && s.OtpHash != GeneratedCode),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_throws_conflict_when_an_active_session_was_just_sent()
    {
        var activeSession = ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            DestinationHash,
            GeneratedOtpHash,
            expiresAt: Now.AddMinutes(9).UtcDateTime,
            createdAt: Now.UtcDateTime);

        sessions.GetActiveByPurposeAndDestinationAsync(
                ContactVerificationPurpose.Register, DestinationHash, Arg.Any<CancellationToken>())
            .Returns(activeSession);

        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register);

        await Assert.ThrowsAsync<ConflictException>(() => sut.RequestAsync(request, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<ContactVerificationSession>(), Arg.Any<CancellationToken>());
        await codeDispatcher.DidNotReceive().SendAsync(
            Arg.Any<VerificationDeliveryChannel>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_cancels_the_stale_active_session_and_issues_a_new_one_after_the_resend_interval()
    {
        var staleSession = ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            DestinationHash,
            GeneratedOtpHash,
            expiresAt: Now.AddMinutes(30).UtcDateTime,
            createdAt: Now.AddMinutes(-5).UtcDateTime);

        sessions.GetActiveByPurposeAndDestinationAsync(
                ContactVerificationPurpose.Register, DestinationHash, Arg.Any<CancellationToken>())
            .Returns(staleSession);

        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register);

        await sut.RequestAsync(request, CancellationToken.None);

        Assert.Equal(ContactVerificationSessionStatus.Cancelled, staleSession.Status);
        await sessions.Received(1).UpdateAsync(staleSession, Arg.Any<CancellationToken>());
        await sessions.Received(1).AddAsync(Arg.Any<ContactVerificationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_throws_conflict_when_the_dispatcher_fails_to_send_the_code()
    {
        sessions.GetActiveByPurposeAndDestinationAsync(
                ContactVerificationPurpose.Register, DestinationHash, Arg.Any<CancellationToken>())
            .Returns((ContactVerificationSession?)null);
        codeDispatcher.SendAsync(
                Arg.Any<VerificationDeliveryChannel>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("proveedor caído"));

        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register);

        await Assert.ThrowsAsync<ConflictException>(() => sut.RequestAsync(request, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<ContactVerificationSession>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_throws_when_claim_purpose_is_missing_the_subject_user()
    {
        var request = new RequestContactEmailVerification(Email, ContactVerificationPurpose.Claim);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.RequestAsync(request, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<ContactVerificationSession>(), Arg.Any<CancellationToken>());
        await codeDispatcher.DidNotReceive().SendAsync(
            Arg.Any<VerificationDeliveryChannel>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
