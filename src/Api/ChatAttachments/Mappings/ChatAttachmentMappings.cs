using Api.ChatAttachments.Dtos;
using Application.ChatAttachments.UseCase;
using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Api.ChatAttachments.Mappings;

public static class ChatAttachmentMappings
{
    public static CreateChatAttachmentCommand ToCommand(this CreateChatAttachmentDto dto)
        => new(
            dto.ChatMessageId,
            dto.FileUrl,
            dto.FileType,
            dto.FileName);

    public static ChatAttachmentResponseDto ToResponse(this ChatAttachmentEntity attachment)
        => new(
            attachment.Id,
            attachment.ChatMessageId,
            attachment.FileUrl,
            attachment.FileType,
            attachment.FileName,
            attachment.CreatedAt);

    public static IReadOnlyCollection<ChatAttachmentResponseDto> ToResponse(
        this IReadOnlyCollection<ChatAttachmentEntity> attachments)
        => attachments.Select(attachment => attachment.ToResponse()).ToArray();
}
