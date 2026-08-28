using Api.ChatConversations.Dtos;
using Api.ChatConversations.Mappings;
using Api.Common.Security;
using Application.ChatConversations.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatConversations.Controllers;

[ApiController]
[Route("api/chat/conversations")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatConversationController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear una conversación de chat")]
    [EndpointDescription("Registra una conversación abierta con estado obligatorio y prioridad opcional. La IA queda habilitada por defecto.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> Create(
        [FromBody] CreateChatConversationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = conversation.ToResponse();
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

    [HttpGet]
    [EndpointSummary("Listar conversaciones de chat")]
    [EndpointDescription("Devuelve todas las conversaciones registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatConversationResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatConversationResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var conversations = await sender.Send(new GetAllChatConversationsQuery(), cancellationToken);
        return Ok(conversations.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener una conversación por su identificador")]
    [EndpointDescription("Devuelve la conversación indicada.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var conversation = await sender.Send(new GetChatConversationByIdQuery(id), cancellationToken);
        return conversation is null
            ? NotFound(new { Message = $"No se encontró la conversación '{id}'." })
            : Ok(conversation.ToResponse());
    }

    [HttpPatch("{id:guid}/status")]
    [EndpointSummary("Cambiar el estado de una conversación")]
    [EndpointDescription("Actualiza el estado de la conversación validando que el catálogo exista.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateChatConversationStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(conversation.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/priority")]
    [EndpointSummary("Cambiar la prioridad de una conversación")]
    [EndpointDescription("Establece o retira la prioridad de la conversación.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> UpdatePriority(
        Guid id,
        [FromBody] UpdateChatConversationPriorityDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(conversation.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/ai-enabled")]
    [EndpointSummary("Activar o desactivar IA en una conversación")]
    [EndpointDescription("Habilita o deshabilita el procesamiento de IA para la conversación.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> UpdateAiEnabled(
        Guid id,
        [FromBody] UpdateChatConversationAiEnabledDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(conversation.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/close")]
    [EndpointSummary("Cerrar una conversación")]
    [EndpointDescription("Marca la conversación como cerrada con fecha UTC y un identificador técnico opcional.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> Close(
        Guid id,
        [FromBody] CloseChatConversationDto? dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = dto is null
                ? new CloseChatConversationCommand(id)
                : dto.ToCommand(id);
            var conversation = await sender.Send(command, cancellationToken);
            return Ok(conversation.ToResponse());
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

    [HttpPatch("{id:guid}/reopen")]
    [EndpointSummary("Reabrir una conversación")]
    [EndpointDescription("Marca la conversación como abierta y limpia los datos de cierre.")]
    [ProducesResponseType(typeof(ChatConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationResponseDto>> Reopen(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await sender.Send(new ReopenChatConversationCommand(id), cancellationToken);
            return Ok(conversation.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
