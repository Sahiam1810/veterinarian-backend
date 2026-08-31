using Api.ChatMessages.Dtos;
using Api.ChatMessages.Mappings;
using Api.Common.Security;
using Application.ChatMessages.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatMessages.Controllers;

[ApiController]
[Route("api/chat/messages")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatMessageController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un mensaje de chat")]
    [EndpointDescription("Registra un mensaje en una conversación y actualiza la fecha del último mensaje.")]
    [ProducesResponseType(typeof(ChatMessageResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageResponseDto>> Create(
        [FromBody] CreateChatMessageDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = message.ToResponse();
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
    [EndpointSummary("Obtener un mensaje de chat por su identificador")]
    [EndpointDescription("Devuelve el mensaje de chat indicado.")]
    [ProducesResponseType(typeof(ChatMessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var message = await sender.Send(new GetChatMessageByIdQuery(id), cancellationToken);
        return message is null
            ? NotFound(new { Message = $"No se encontró el mensaje '{id}'." })
            : Ok(message.ToResponse());
    }

    [HttpGet("conversation/{chatConversationId:guid}")]
    [EndpointSummary("Listar mensajes por conversación")]
    [EndpointDescription("Devuelve todos los mensajes asociados a la conversación indicada.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatMessageResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatMessageResponseDto>>> GetByConversationId(
        Guid chatConversationId,
        CancellationToken cancellationToken)
    {
        var messages = await sender.Send(
            new GetChatMessagesByConversationIdQuery(chatConversationId),
            cancellationToken);
        return Ok(messages.ToResponse());
    }
}
