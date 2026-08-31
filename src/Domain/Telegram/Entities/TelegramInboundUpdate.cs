using Domain.Common;
using Domain.Telegram.Enums;

namespace Domain.Telegram.Entities;

public sealed class TelegramInboundUpdate : BaseEntity<long>
{
    private TelegramInboundUpdate()
    {
    }

    public long TelegramUserId { get; private set; }

    public long TelegramChatId { get; private set; }

    public long TelegramMessageId { get; private set; }

    public string ChatType { get; private set; } = null!;

    public string? MessageText { get; private set; }

    public string? ResponseText { get; private set; }

    public TelegramInboundUpdateStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime NextAttemptAt { get; private set; }

    public int LastSentChunkIndex { get; private set; }

    public string? LastErrorCode { get; private set; }

    public static TelegramInboundUpdate Create(
        long updateId,
        long telegramUserId,
        long telegramChatId,
        long telegramMessageId,
        string chatType,
        string? messageText,
        DateTime createdAt)
    {
        EnsureExternalId(updateId, nameof(updateId));
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));
        EnsureExternalId(telegramMessageId, nameof(telegramMessageId));

        if (string.IsNullOrWhiteSpace(chatType))
        {
            throw new ArgumentException(
                "El tipo de chat es obligatorio.",
                nameof(chatType));
        }

        return new TelegramInboundUpdate
        {
            Id = updateId,
            TelegramUserId = telegramUserId,
            TelegramChatId = telegramChatId,
            TelegramMessageId = telegramMessageId,
            ChatType = chatType.Trim(),
            MessageText = messageText,
            Status = TelegramInboundUpdateStatus.Pending,
            Attempts = 0,
            NextAttemptAt = createdAt,
            LastSentChunkIndex = -1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Claim(DateTime claimedAt)
    {
        if (Status != TelegramInboundUpdateStatus.Pending ||
            claimedAt < NextAttemptAt)
        {
            throw new InvalidOperationException(
                "La actualización no está disponible para procesamiento.");
        }

        Status = TelegramInboundUpdateStatus.Processing;
        Attempts++;
        UpdatedAt = claimedAt;
    }

    public void PrepareResponse(string responseText, DateTime preparedAt)
    {
        if (Status != TelegramInboundUpdateStatus.Processing)
        {
            throw new InvalidOperationException(
                "Solo una actualización en procesamiento puede preparar respuesta.");
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new ArgumentException(
                "La respuesta de Telegram es obligatoria.",
                nameof(responseText));
        }

        ResponseText = responseText;
        Status = TelegramInboundUpdateStatus.Prepared;
        UpdatedAt = preparedAt;
    }

    public void ConfirmChunk(int chunkIndex, DateTime confirmedAt)
    {
        if (Status != TelegramInboundUpdateStatus.Prepared ||
            chunkIndex != LastSentChunkIndex + 1)
        {
            throw new InvalidOperationException(
                "Los fragmentos deben confirmarse una sola vez y en orden.");
        }

        LastSentChunkIndex = chunkIndex;
        UpdatedAt = confirmedAt;
    }

    public void Complete(DateTime completedAt)
    {
        if (Status != TelegramInboundUpdateStatus.Prepared)
        {
            throw new InvalidOperationException(
                "Solo una actualización preparada puede completarse.");
        }

        Status = TelegramInboundUpdateStatus.Completed;
        MessageText = null;
        ResponseText = null;
        LastErrorCode = null;
        UpdatedAt = completedAt;
    }

    public void ScheduleRetry(
        DateTime nextAttemptAt,
        string errorCode,
        int maximumAttempts,
        DateTime failedAt)
    {
        if (Status is not TelegramInboundUpdateStatus.Processing and
            not TelegramInboundUpdateStatus.Prepared)
        {
            throw new InvalidOperationException(
                "Solo una actualización activa puede reintentarse.");
        }

        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException(
                "El código seguro del error es obligatorio.",
                nameof(errorCode));
        }

        LastErrorCode = errorCode;
        UpdatedAt = failedAt;

        if (Attempts >= maximumAttempts)
        {
            Status = TelegramInboundUpdateStatus.Failed;
            MessageText = null;
            ResponseText = null;
            return;
        }

        if (nextAttemptAt <= failedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextAttemptAt),
                "El siguiente intento debe ser posterior al fallo.");
        }

        Status = TelegramInboundUpdateStatus.Pending;
        NextAttemptAt = nextAttemptAt;
    }

    private static void EnsureExternalId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El identificador externo debe ser positivo.");
        }
    }
}
