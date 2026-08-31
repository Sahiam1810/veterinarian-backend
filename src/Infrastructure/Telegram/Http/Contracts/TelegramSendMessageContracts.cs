using System.Text.Json.Serialization;

namespace Infrastructure.Telegram.Http.Contracts;

internal sealed record TelegramSendMessageRequest(
    [property: JsonPropertyName("chat_id")] long ChatId,
    [property: JsonPropertyName("text")] string Text);

internal sealed record TelegramSendMessageResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("result")] TelegramSentMessage? Result);

internal sealed record TelegramSentMessage(
    [property: JsonPropertyName("message_id")] long MessageId);
