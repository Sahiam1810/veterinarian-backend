using Api.ChatEscalations.Dtos;
using Api.ChatEscalations.Mappings;
using Api.Common.Security;
using Application.ChatEscalations.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatEscalations.Controllers;

[ApiController]
[Route("api/chat/escalations")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatEscalationController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un escalamiento de chat")]
    [EndpointDescription("Registra un escalamiento asociado a una conversación existente.")]
    [ProducesResponseType(typeof(ChatEscalationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationResponseDto>> Create(
        [FromBody] CreateChatEscalationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var escalation = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = escalation.ToResponse();
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
    [EndpointSummary("Listar escalamientos de chat")]
    [EndpointDescription("Devuelve todos los escalamientos registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var escalations = await sender.Send(new GetAllChatEscalationsQuery(), cancellationToken);
        return Ok(escalations.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener escalamiento por identificador")]
    [EndpointDescription("Devuelve el escalamiento indicado.")]
    [ProducesResponseType(typeof(ChatEscalationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var escalation = await sender.Send(new GetChatEscalationByIdQuery(id), cancellationToken);
        return escalation is null
            ? NotFound(new { Message = $"No se encontró el escalamiento '{id}'." })
            : Ok(escalation.ToResponse());
    }

    [HttpGet("by-conversation/{chatConversationId:guid}")]
    [EndpointSummary("Consultar escalamientos por conversación")]
    [EndpointDescription("Devuelve los escalamientos asociados a la conversación indicada.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationResponseDto>>> GetByConversationId(
        Guid chatConversationId,
        CancellationToken cancellationToken)
    {
        var escalations = await sender.Send(
            new GetChatEscalationsByConversationIdQuery(chatConversationId),
            cancellationToken);
        return Ok(escalations.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar un escalamiento de chat")]
    [EndpointDescription("Actualiza el estado, origen y motivo del escalamiento.")]
    [ProducesResponseType(typeof(ChatEscalationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationResponseDto>> Update(
        Guid id,
        [FromBody] UpdateChatEscalationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var escalation = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(escalation.ToResponse());
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

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Eliminar un escalamiento de chat")]
    [EndpointDescription("Elimina el escalamiento indicado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatEscalationCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
