using Domain.Verification.Entities;
using Domain.Verification.Enums;
using Xunit;

namespace Application.Tests.Verification;

public sealed class AppointmentActionVerificationSessionTests
{
    private static readonly DateTime Now = new(2026, 9, 2, 15, 0, 0, DateTimeKind.Utc);
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Start_creates_awaiting_otp_session()
    {
        var session = AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            Hash,
            Hash,
            Now.AddMinutes(5),
            Now);

        Assert.Equal(VerificationSessionStatus.AwaitingOtp, session.Status);
        Assert.Equal(0, session.Attempts);
        Assert.NotNull(session.OtpHash);
    }

    [Fact]
    public void RegisterFailedAttempt_blocks_at_maximum()
    {
        var session = AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            Hash,
            Hash,
            Now.AddMinutes(5),
            Now);

        session.RegisterFailedAttempt(2, Now.AddSeconds(1));
        Assert.Equal(VerificationSessionStatus.AwaitingOtp, session.Status);

        session.RegisterFailedAttempt(2, Now.AddSeconds(2));
        Assert.Equal(VerificationSessionStatus.Blocked, session.Status);
        Assert.Null(session.OtpHash);
    }

    [Fact]
    public void Complete_clears_otp_hash()
    {
        var session = AppointmentActionVerificationSession.Start(
            Guid.NewGuid(),
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            Hash,
            Hash,
            Now.AddMinutes(5),
            Now);

        session.Complete(Now.AddSeconds(10));
        Assert.Equal(VerificationSessionStatus.Completed, session.Status);
        Assert.Null(session.OtpHash);
    }
}
