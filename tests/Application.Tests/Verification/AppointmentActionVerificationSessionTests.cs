using Domain.Verification.Entities;
using Domain.Verification.Enums;
using Xunit;

namespace Application.Tests.Verification;

public sealed class AppointmentActionVerificationSessionTests
{
    private const string ValidHash = "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string ValidOtpHash = "CDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCD";

    private static readonly DateTime CreatedAt = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresAt = CreatedAt.AddMinutes(10);

    [Fact]
    public void Start_creates_an_awaiting_otp_session_with_the_expected_fields()
    {
        var appointmentId = Guid.NewGuid();

        var session = AppointmentActionVerificationSession.Start(
            appointmentId,
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt,
            actionPayload: "{}");

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(appointmentId, session.AppointmentId);
        Assert.Equal(AppointmentVerificationAction.Cancel, session.Action);
        Assert.Equal(VerificationDeliveryChannel.Sms, session.Channel);
        Assert.Equal(ValidHash, session.DestinationHash);
        Assert.Equal(ValidOtpHash, session.OtpHash);
        Assert.Equal(VerificationSessionStatus.AwaitingOtp, session.Status);
        Assert.Equal(0, session.Attempts);
        Assert.Equal(ExpiresAt, session.ExpiresAt);
        Assert.Equal("{}", session.ActionPayload);
        Assert.Equal(CreatedAt, session.CreatedAt);
        Assert.Equal(CreatedAt, session.UpdatedAt);
    }

    [Fact]
    public void Start_throws_when_the_appointment_id_is_empty()
    {
        Assert.Throws<ArgumentException>(() => AppointmentActionVerificationSession.Start(
            Guid.Empty,
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tooshort")]
    public void Start_throws_when_the_destination_hash_is_not_a_valid_sha256_hex_string(string? invalidHash)
    {
        Assert.Throws<ArgumentException>(() => AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            invalidHash!,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tooshort")]
    public void Start_throws_when_the_otp_hash_is_not_a_valid_sha256_hex_string(string? invalidHash)
    {
        Assert.Throws<ArgumentException>(() => AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            ValidHash,
            invalidHash!,
            ExpiresAt,
            CreatedAt));
    }

    [Fact]
    public void Start_throws_when_the_expiration_is_not_after_the_creation_instant()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            ValidHash,
            ValidOtpHash,
            expiresAt: CreatedAt,
            createdAt: CreatedAt));
    }

    [Fact]
    public void RegisterFailedAttempt_increments_attempts_and_stays_awaiting_otp_below_the_limit()
    {
        var session = CreateActiveSession();

        session.RegisterFailedAttempt(maximumAttempts: 3, attemptedAt: CreatedAt.AddMinutes(1));

        Assert.Equal(1, session.Attempts);
        Assert.Equal(VerificationSessionStatus.AwaitingOtp, session.Status);
        Assert.Equal(ValidOtpHash, session.OtpHash);
        Assert.Equal(CreatedAt.AddMinutes(1), session.UpdatedAt);
    }

    [Fact]
    public void RegisterFailedAttempt_blocks_the_session_and_clears_the_otp_hash_once_the_limit_is_reached()
    {
        var session = CreateActiveSession();

        session.RegisterFailedAttempt(maximumAttempts: 2, attemptedAt: CreatedAt.AddMinutes(1));
        session.RegisterFailedAttempt(maximumAttempts: 2, attemptedAt: CreatedAt.AddMinutes(2));

        Assert.Equal(2, session.Attempts);
        Assert.Equal(VerificationSessionStatus.Blocked, session.Status);
        Assert.Null(session.OtpHash);
    }

    [Fact]
    public void RegisterFailedAttempt_throws_when_maximum_attempts_is_not_positive()
    {
        var session = CreateActiveSession();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => session.RegisterFailedAttempt(maximumAttempts: 0, attemptedAt: CreatedAt.AddMinutes(1)));
    }

    [Fact]
    public void RegisterFailedAttempt_throws_when_the_session_already_expired()
    {
        var session = CreateActiveSession();

        Assert.Throws<InvalidOperationException>(
            () => session.RegisterFailedAttempt(maximumAttempts: 3, attemptedAt: ExpiresAt));
    }

    [Fact]
    public void RegisterFailedAttempt_throws_when_the_session_is_not_awaiting_otp()
    {
        var session = CreateActiveSession();
        session.Cancel(CreatedAt.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => session.RegisterFailedAttempt(maximumAttempts: 3, attemptedAt: CreatedAt.AddMinutes(2)));
    }

    [Fact]
    public void Complete_marks_the_session_completed_and_clears_the_otp_hash()
    {
        var session = CreateActiveSession();

        session.Complete(CreatedAt.AddMinutes(1));

        Assert.Equal(VerificationSessionStatus.Completed, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Equal(CreatedAt.AddMinutes(1), session.UpdatedAt);
    }

    [Fact]
    public void Complete_throws_when_the_session_already_expired()
    {
        var session = CreateActiveSession();

        Assert.Throws<InvalidOperationException>(() => session.Complete(ExpiresAt));
    }

    [Fact]
    public void Complete_throws_when_the_session_is_not_awaiting_otp()
    {
        var session = CreateActiveSession();
        session.Cancel(CreatedAt.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => session.Complete(CreatedAt.AddMinutes(2)));
    }

    [Fact]
    public void Cancel_marks_the_session_cancelled_and_clears_the_otp_hash()
    {
        var session = CreateActiveSession();

        session.Cancel(CreatedAt.AddMinutes(1));

        Assert.Equal(VerificationSessionStatus.Cancelled, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Equal(CreatedAt.AddMinutes(1), session.UpdatedAt);
    }

    [Fact]
    public void Cancel_throws_when_the_session_is_not_awaiting_otp()
    {
        var session = CreateActiveSession();
        session.Cancel(CreatedAt.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => session.Cancel(CreatedAt.AddMinutes(2)));
    }

    [Fact]
    public void Expire_marks_the_session_expired_once_past_the_expiration_instant()
    {
        var session = CreateActiveSession();

        session.Expire(ExpiresAt);

        Assert.Equal(VerificationSessionStatus.Expired, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Equal(ExpiresAt, session.UpdatedAt);
    }

    [Fact]
    public void Expire_throws_when_the_session_has_not_expired_yet()
    {
        var session = CreateActiveSession();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Expire(ExpiresAt.AddMinutes(-1)));
    }

    [Fact]
    public void Expire_throws_when_the_session_is_not_awaiting_otp()
    {
        var session = CreateActiveSession();
        session.Cancel(CreatedAt.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => session.Expire(ExpiresAt));
    }

    private static AppointmentActionVerificationSession CreateActiveSession() =>
        AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt);
}
