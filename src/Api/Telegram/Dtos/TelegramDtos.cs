using System.Text.Json.Serialization;

namespace Api.Telegram.Dtos;

public sealed record CreateTelegramLinkCodeResponse(
    string Code,
    string DeepLink,
    DateTimeOffset ExpiresAt);

public sealed record TelegramUpdateRequest(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMessageRequest? Message);

public sealed record TelegramMessageRequest(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("from")] TelegramFromRequest? From,
    [property: JsonPropertyName("chat")] TelegramChatRequest Chat,
    [property: JsonPropertyName("text")] string? Text);

public sealed record TelegramFromRequest(
    [property: JsonPropertyName("id")] long Id);

public sealed record TelegramChatRequest(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type);
