using Api.ChatParticipants.Dtos;
using Api.ChatParticipants.Mappings;
using Api.Common.Security;
using Application.ChatParticipants.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatParticipants.Controllers;

[ApiController]
[Route("api/chat/participants")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatParticipantController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un participante de chat")]
    [EndpointDescription("Registra un participante en una conversación con exactamente una identidad (perfil, agente humano o modelo de IA).")]
    [ProducesResponseType(typeof(ChatParticipantResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatParticipantResponseDto>> Create(
        [FromBody] CreateChatParticipantDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var participant = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = participant.ToResponse();
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
    [EndpointSummary("Obtener un participante de chat por su identificador")]
    [EndpointDescription("Devuelve el participante de chat indicado.")]
    [ProducesResponseType(typeof(ChatParticipantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatParticipantResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var participant = await sender.Send(new GetChatParticipantByIdQuery(id), cancellationToken);
        return participant is null
            ? NotFound(new { Message = $"No se encontró el participante '{id}'." })
            : Ok(participant.ToResponse());
    }

    [HttpGet("conversation/{chatConversationId:guid}")]
    [EndpointSummary("Listar participantes por conversación")]
    [EndpointDescription("Devuelve todos los participantes asociados a la conversación indicada.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatParticipantResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatParticipantResponseDto>>> GetByConversationId(
        Guid chatConversationId,
        CancellationToken cancellationToken)
    {
        var participants = await sender.Send(
            new GetChatParticipantsByConversationIdQuery(chatConversationId),
            cancellationToken);
        return Ok(participants.ToResponse());
    }

    [HttpPatch("{id:guid}/identity")]
    [EndpointSummary("Cambiar la identidad de un participante")]
    [EndpointDescription("Actualiza la identidad del participante manteniendo exactamente una identidad válida.")]
    [ProducesResponseType(typeof(ChatParticipantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatParticipantResponseDto>> ChangeIdentity(
        Guid id,
        [FromBody] ChangeChatParticipantIdentityDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var participant = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(participant.ToResponse());
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
}
