using Api.ChatAttachments.Dtos;
using Api.ChatAttachments.Mappings;
using Api.Common.Security;
using Application.ChatAttachments.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatAttachments.Controllers;

[ApiController]
[Route("api/chat/attachments")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatAttachmentController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un adjunto de chat")]
    [EndpointDescription("Registra un adjunto asociado a un mensaje de chat.")]
    [ProducesResponseType(typeof(ChatAttachmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAttachmentResponseDto>> Create(
        [FromBody] CreateChatAttachmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = attachment.ToResponse();
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener un adjunto de chat por su identificador")]
    [EndpointDescription("Devuelve el adjunto de chat indicado.")]
    [ProducesResponseType(typeof(ChatAttachmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAttachmentResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var attachment = await sender.Send(new GetChatAttachmentByIdQuery(id), cancellationToken);
        return attachment is null
            ? NotFound(new { Message = $"No se encontró el adjunto '{id}'." })
            : Ok(attachment.ToResponse());
    }

    [HttpGet("message/{chatMessageId:guid}")]
    [EndpointSummary("Listar adjuntos por mensaje")]
    [EndpointDescription("Devuelve todos los adjuntos asociados al mensaje indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatAttachmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatAttachmentResponseDto>>> GetByMessageId(
        Guid chatMessageId,
        CancellationToken cancellationToken)
    {
        var attachments = await sender.Send(
            new GetChatAttachmentsByMessageIdQuery(chatMessageId),
            cancellationToken);
        return Ok(attachments.ToResponse());
    }
}
