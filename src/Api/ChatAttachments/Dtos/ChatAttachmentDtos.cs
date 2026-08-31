namespace Api.ChatAttachments.Dtos;

public sealed record CreateChatAttachmentDto(
    Guid ChatMessageId,
    string FileUrl,
    string FileType,
    string FileName);

public sealed record ChatAttachmentResponseDto(
    Guid Id,
    Guid ChatMessageId,
    string FileUrl,
    string FileType,
    string FileName,
    DateTime CreatedAt);
