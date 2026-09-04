using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Xunit;

namespace Application.Tests.Telegram.Domain;

public sealed class TelegramIdentitySessionTests
{
    private static readonly DateTime Now =
        new(2026, 9, 4, 15, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string OtpHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Start_waits_for_identification_and_keeps_pending_update_reference()
    {
        var session = TelegramIdentitySession.Start(1001, 2002, 42, Now);

        Assert.Equal(TelegramIdentitySessionStatus.AwaitingIdentification, session.Status);
        Assert.Equal(1001, session.TelegramUserId);
        Assert.Equal(2002, session.TelegramChatId);
        Assert.Equal(42, session.PendingInboundUpdateId);
    }

    [Fact]
    public void Known_client_otp_can_verify_access_with_both_expirations()
    {
        var session = TelegramIdentitySession.Start(1001, 2002, 42, Now);
        session.BeginKnownClientOtp(PersonId, OtpHash, Now.AddMinutes(5), Now);

        session.Verify(PersonId, Now.AddHours(24), Now.AddMinutes(30), Now.AddMinutes(1));

        Assert.True(session.IsAccessValid(Now.AddMinutes(29)));
        Assert.False(session.IsAccessValid(Now.AddMinutes(30)));
        Assert.False(session.IsAccessValid(Now.AddHours(24)));
        Assert.Null(session.OtpHash);
    }

    [Fact]
    public void Unknown_client_registration_collects_protected_values_before_otp()
    {
        var session = TelegramIdentitySession.Start(1001, 2002, 42, Now);

        session.RequireRegistration("protected-id", Now.AddSeconds(1));
        session.ConfirmRegistration(Now.AddSeconds(2));
        session.CaptureFullName("protected-name", Now.AddSeconds(3));
        session.BeginRegistrationOtp(
            "protected-email",
            OtpHash,
            Now.AddMinutes(5),
            Now.AddSeconds(4));

        Assert.Equal(TelegramIdentitySessionStatus.AwaitingOtp, session.Status);
        Assert.Equal("protected-id", session.ProtectedIdentification);
        Assert.Equal("protected-name", session.ProtectedFullName);
        Assert.Equal("protected-email", session.ProtectedEmail);
        Assert.Null(session.PersonId);
    }

    [Fact]
    public void Invalid_otp_attempts_block_the_session_at_the_configured_limit()
    {
        var session = TelegramIdentitySession.Start(1001, 2002, 42, Now);
        session.BeginKnownClientOtp(PersonId, OtpHash, Now.AddMinutes(5), Now);

        session.RegisterFailedOtpAttempt(2, Now.AddSeconds(1));
        session.RegisterFailedOtpAttempt(2, Now.AddSeconds(2));

        Assert.Equal(TelegramIdentitySessionStatus.Blocked, session.Status);
        Assert.Null(session.OtpHash);
    }

    [Fact]
    public void Touch_extends_idle_access_without_extending_the_absolute_limit()
    {
        var session = TelegramIdentitySession.Start(1001, 2002, 42, Now);
        session.BeginKnownClientOtp(PersonId, OtpHash, Now.AddMinutes(5), Now);
        session.Verify(PersonId, Now.AddHours(24), Now.AddMinutes(30), Now.AddMinutes(1));

        session.Touch(Now.AddHours(30), Now.AddMinutes(20));

        Assert.Equal(Now.AddHours(24), session.AbsoluteExpiresAt);
        Assert.Equal(Now.AddHours(24), session.IdleExpiresAt);
    }

    [Fact]
    public void Verified_session_re_consumes_pending_update_only_once()
    {
        var session = TelegramIdentitySession.Start(1001, 2002, 42, Now);
        session.BeginKnownClientOtp(PersonId, OtpHash, Now.AddMinutes(5), Now);
        session.Verify(PersonId, Now.AddHours(24), Now.AddMinutes(30), Now.AddMinutes(1));

        Assert.Equal(42, session.TakePendingInboundUpdate(Now.AddMinutes(2)));
        Assert.Null(session.TakePendingInboundUpdate(Now.AddMinutes(3)));
    }
}
