using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Xunit;

namespace Application.Tests.Telegram.Domain;

public sealed class TelegramRegistrationSessionTests
{
    private static readonly DateTime Now = new(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string TokenHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Start_waits_for_email()
    {
        var session = TelegramRegistrationSession.Start(1001, 2001, Now);

        Assert.Equal(TelegramRegistrationSessionStatus.AwaitingEmail, session.Status);
        Assert.Equal(1001, session.TelegramUserId);
        Assert.Equal(2001, session.TelegramChatId);
    }

    [Fact]
    public void New_account_moves_from_valid_otp_to_awaiting_profile()
    {
        var session = OtpSession(TelegramRegistrationAccountKind.New);

        session.VerifyOtp(Now.AddMinutes(1));
        session.IssueCompletionToken(TokenHash, Now.AddMinutes(16), Now.AddMinutes(1));

        Assert.Equal(TelegramRegistrationSessionStatus.AwaitingProfile, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Equal(TokenHash, session.CompletionTokenHash);
    }

    [Fact]
    public void Active_account_can_complete_after_otp_without_profile_form()
    {
        var session = OtpSession(TelegramRegistrationAccountKind.Active, PersonId);

        session.VerifyOtp(Now.AddMinutes(1));
        session.Complete(PersonId, Now.AddMinutes(1));

        Assert.Equal(TelegramRegistrationSessionStatus.Completed, session.Status);
        Assert.Equal(PersonId, session.PersonId);
    }

    [Fact]
    public void Maximum_failed_attempt_blocks_and_clears_otp()
    {
        var session = OtpSession(TelegramRegistrationAccountKind.New);

        session.RegisterFailedOtp(1, Now.AddSeconds(1));

        Assert.Equal(TelegramRegistrationSessionStatus.Blocked, session.Status);
        Assert.Null(session.OtpHash);
    }

    [Fact]
    public void Completion_token_can_only_be_consumed_once()
    {
        var session = ProfileSession();
        session.Complete(PersonId, Now.AddMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            session.Complete(PersonId, Now.AddMinutes(3)));
    }

    [Fact]
    public void Expired_otp_cannot_be_verified()
    {
        var session = OtpSession(TelegramRegistrationAccountKind.New);

        Assert.Throws<InvalidOperationException>(() =>
            session.VerifyOtp(Now.AddMinutes(5)));
    }

    private static TelegramRegistrationSession OtpSession(
        TelegramRegistrationAccountKind accountKind,
        Guid? personId = null)
    {
        var session = TelegramRegistrationSession.Start(1001, 2001, Now);
        session.PrepareOtp(
            "protected-email",
            Hash,
            Hash,
            accountKind,
            personId,
            Now.AddMinutes(5),
            Now);
        return session;
    }

    private static TelegramRegistrationSession ProfileSession()
    {
        var session = OtpSession(TelegramRegistrationAccountKind.New);
        session.VerifyOtp(Now.AddMinutes(1));
        session.IssueCompletionToken(TokenHash, Now.AddMinutes(16), Now.AddMinutes(1));
        return session;
    }
}
