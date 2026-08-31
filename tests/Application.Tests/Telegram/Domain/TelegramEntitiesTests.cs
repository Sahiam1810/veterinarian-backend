using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Xunit;

namespace Application.Tests.Telegram.Domain;

public sealed class TelegramEntitiesTests
{
    private static readonly DateTime Now =
        new(2026, 8, 31, 18, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserLinkId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ConversationId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string CodeHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Link_code_can_only_be_consumed_once()
    {
        var code = TelegramLinkCode.Create(
            PersonId,
            CodeHash,
            Now.AddMinutes(10),
            Now);

        code.Consume(Now.AddMinutes(1));

        Assert.Equal(Now.AddMinutes(1), code.ConsumedAt);
        Assert.Throws<InvalidOperationException>(
            () => code.Consume(Now.AddMinutes(2)));
    }

    [Fact]
    public void Expired_link_code_cannot_be_consumed()
    {
        var code = TelegramLinkCode.Create(
            PersonId,
            CodeHash,
            Now.AddMinutes(10),
            Now);

        Assert.Throws<InvalidOperationException>(
            () => code.Consume(Now.AddMinutes(10)));
    }

    [Fact]
    public void Invalidated_link_code_is_no_longer_active()
    {
        var code = TelegramLinkCode.Create(
            PersonId,
            CodeHash,
            Now.AddMinutes(10),
            Now);

        code.Invalidate(Now.AddMinutes(1));

        Assert.False(code.IsActiveAt(Now.AddMinutes(2)));
        Assert.Equal(Now.AddMinutes(1), code.InvalidatedAt);
    }

    [Fact]
    public void User_link_can_be_relinked_to_another_private_chat()
    {
        var link = TelegramUserLink.Create(PersonId, 1001, 1001, Now);

        link.Relink(2002, 2002, Now.AddMinutes(1));

        Assert.Equal(2002, link.TelegramUserId);
        Assert.Equal(2002, link.TelegramChatId);
        Assert.Equal(Now.AddMinutes(1), link.UpdatedAt);
    }

    [Fact]
    public void User_link_can_be_revoked_and_relinked()
    {
        var link = TelegramUserLink.Create(PersonId, 1001, 1001, Now);

        link.Revoke(Now.AddMinutes(1));

        Assert.False(link.IsActive);
        Assert.Equal(Now.AddMinutes(1), link.UnlinkedAt);

        link.Relink(2002, 2002, Now.AddMinutes(2));

        Assert.True(link.IsActive);
        Assert.Null(link.UnlinkedAt);
    }

    [Fact]
    public void Conversation_link_can_move_to_a_new_internal_conversation()
    {
        var link = TelegramConversationLink.Create(
            UserLinkId,
            ConversationId,
            Now);
        var nextConversationId =
            Guid.Parse("44444444-4444-4444-4444-444444444444");

        link.BindConversation(nextConversationId, Now.AddMinutes(1));

        Assert.Equal(nextConversationId, link.ConversationId);
        Assert.Equal(Now.AddMinutes(1), link.UpdatedAt);
    }

    [Fact]
    public void New_inbound_update_is_pending_without_delivery_progress()
    {
        var update = CreateUpdate();

        Assert.Equal(TelegramInboundUpdateStatus.Pending, update.Status);
        Assert.Equal(0, update.Attempts);
        Assert.Equal(-1, update.LastSentChunkIndex);
        Assert.Equal(Now, update.NextAttemptAt);
    }

    [Fact]
    public void Completed_update_clears_transient_and_error_texts()
    {
        var update = CreateUpdate();
        update.Claim(Now);
        update.PrepareResponse("respuesta", Now.AddSeconds(1));
        update.ConfirmChunk(0, Now.AddSeconds(2));

        update.Complete(Now.AddSeconds(3));

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        Assert.Null(update.MessageText);
        Assert.Null(update.ResponseText);
        Assert.Null(update.LastErrorCode);
    }

    [Fact]
    public void Chunk_confirmation_must_be_strictly_sequential()
    {
        var update = CreateUpdate();
        update.Claim(Now);
        update.PrepareResponse("respuesta", Now.AddSeconds(1));

        Assert.Throws<InvalidOperationException>(
            () => update.ConfirmChunk(1, Now.AddSeconds(2)));
    }

    [Fact]
    public void Retry_returns_update_to_pending_with_next_attempt()
    {
        var update = CreateUpdate();
        update.Claim(Now);
        var nextAttempt = Now.AddSeconds(2);

        update.ScheduleRetry(
            nextAttempt,
            "agent_unavailable",
            maximumAttempts: 3,
            Now.AddSeconds(1));

        Assert.Equal(TelegramInboundUpdateStatus.Pending, update.Status);
        Assert.Equal(1, update.Attempts);
        Assert.Equal(nextAttempt, update.NextAttemptAt);
        Assert.Equal("agent_unavailable", update.LastErrorCode);
    }

    [Fact]
    public void Final_failed_attempt_clears_transient_texts()
    {
        var update = CreateUpdate();
        update.Claim(Now);

        update.ScheduleRetry(
            Now.AddSeconds(2),
            "agent_unavailable",
            maximumAttempts: 1,
            Now.AddSeconds(1));

        Assert.Equal(TelegramInboundUpdateStatus.Failed, update.Status);
        Assert.Null(update.MessageText);
        Assert.Null(update.ResponseText);
        Assert.Equal("agent_unavailable", update.LastErrorCode);
    }

    [Fact]
    public void Linking_session_starts_waiting_for_email()
    {
        var session = TelegramLinkingSession.Start(1001, 1001, Now);

        Assert.Equal(TelegramLinkingSessionStatus.AwaitingEmail, session.Status);
        Assert.Equal(1001, session.TelegramUserId);
        Assert.Equal(1001, session.TelegramChatId);
        Assert.Equal(0, session.Attempts);
        Assert.Null(session.PersonId);
    }

    [Fact]
    public void Linking_session_blocks_after_fifth_invalid_otp()
    {
        var session = TelegramLinkingSession.Start(1001, 1001, Now);
        session.ResolveAccount(
            PersonId,
            CodeHash,
            CodeHash,
            Now.AddMinutes(5),
            Now);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            session.RegisterFailedAttempt(5, Now.AddSeconds(attempt + 1));
        }

        Assert.Equal(TelegramLinkingSessionStatus.Blocked, session.Status);
        Assert.Equal(5, session.Attempts);
    }

    [Fact]
    public void Linking_session_completes_only_once()
    {
        var session = TelegramLinkingSession.Start(1001, 1001, Now);
        session.ResolveAccount(
            PersonId,
            CodeHash,
            CodeHash,
            Now.AddMinutes(5),
            Now);

        session.Complete(Now.AddMinutes(1));

        Assert.Equal(TelegramLinkingSessionStatus.Linked, session.Status);
        Assert.Throws<InvalidOperationException>(
            () => session.Complete(Now.AddMinutes(2)));
    }

    [Fact]
    public void Expired_otp_session_cannot_be_completed()
    {
        var session = TelegramLinkingSession.Start(1001, 1001, Now);
        session.ResolveAccount(
            PersonId,
            CodeHash,
            CodeHash,
            Now.AddMinutes(5),
            Now);

        session.Expire(Now.AddMinutes(5));

        Assert.Equal(TelegramLinkingSessionStatus.Expired, session.Status);
        Assert.Throws<InvalidOperationException>(
            () => session.Complete(Now.AddMinutes(5)));
    }

    [Fact]
    public void Processing_update_can_redact_sensitive_message_text()
    {
        var update = CreateUpdate();
        update.Claim(Now);

        update.RedactSensitiveText(Now.AddSeconds(1));

        Assert.Null(update.MessageText);
        Assert.Equal(TelegramInboundUpdateStatus.Processing, update.Status);
        Assert.Equal(Now.AddSeconds(1), update.UpdatedAt);
    }

    [Theory]
    [InlineData(0L, 1001L, 1001L, 7L)]
    [InlineData(42L, 0L, 1001L, 7L)]
    [InlineData(42L, 1001L, 0L, 7L)]
    [InlineData(42L, 1001L, 1001L, 0L)]
    public void Inbound_update_rejects_non_positive_external_ids(
        long updateId,
        long telegramUserId,
        long telegramChatId,
        long telegramMessageId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TelegramInboundUpdate.Create(
                updateId,
                telegramUserId,
                telegramChatId,
                telegramMessageId,
                "private",
                "hola",
                Now));
    }

    private static TelegramInboundUpdate CreateUpdate() =>
        TelegramInboundUpdate.Create(
            42,
            1001,
            1001,
            7,
            "private",
            "hola",
            Now);
}
