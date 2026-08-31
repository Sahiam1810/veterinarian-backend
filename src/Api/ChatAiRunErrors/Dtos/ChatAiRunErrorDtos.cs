namespace Api.ChatAiRunErrors.Dtos;

public sealed record CreateChatAiRunErrorDto(
    Guid ChatAiRunId,
    string ErrorMessage,
    string? ErrorCode,
    string? ProviderErrorId);

public sealed record ChatAiRunErrorResponseDto(
    Guid Id,
    Guid ChatAiRunId,
    string ErrorMessage,
    string? ErrorCode,
    string? ProviderErrorId,
    DateTime CreatedAt);
