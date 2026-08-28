using Api.ChatConversationAssignments.Dtos;
using Api.ChatConversationAssignments.Mappings;
using Api.Common.Security;
using Application.ChatConversationAssignments.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatConversationAssignments.Controllers;

[ApiController]
[Route("api/chat/conversation-assignments")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatConversationAssignmentController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear una asignación de conversación")]
    [EndpointDescription("Registra la asignación de agente humano para una conversación existente.")]
    [ProducesResponseType(typeof(ChatConversationAssignmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAssignmentResponseDto>> Create(
        [FromBody] CreateChatConversationAssignmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = assignment.ToResponse();
            return CreatedAtAction(
                nameof(GetByConversationId),
                new { chatConversationId = response.ChatConversationId },
                response);
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
    [EndpointSummary("Listar asignaciones de conversación")]
    [EndpointDescription("Devuelve todas las asignaciones registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatConversationAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatConversationAssignmentResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var assignments = await sender.Send(new GetAllChatConversationAssignmentsQuery(), cancellationToken);
        return Ok(assignments.ToResponse());
    }

    [HttpGet("{chatConversationId:guid}")]
    [EndpointSummary("Obtener asignación por conversación")]
    [EndpointDescription("Devuelve la asignación asociada a la conversación indicada.")]
    [ProducesResponseType(typeof(ChatConversationAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAssignmentResponseDto>> GetByConversationId(
        Guid chatConversationId,
        CancellationToken cancellationToken)
    {
        var assignment = await sender.Send(
            new GetChatConversationAssignmentByConversationIdQuery(chatConversationId),
            cancellationToken);

        return assignment is null
            ? NotFound(new { Message = $"No se encontró la asignación de la conversación '{chatConversationId}'." })
            : Ok(assignment.ToResponse());
    }

    [HttpGet("by-agent/{agentHumanId:guid}")]
    [EndpointSummary("Consultar asignaciones por agente humano")]
    [EndpointDescription("Devuelve las asignaciones asociadas al agente humano indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatConversationAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatConversationAssignmentResponseDto>>> GetByAgentHumanId(
        Guid agentHumanId,
        CancellationToken cancellationToken)
    {
        var assignments = await sender.Send(
            new GetChatConversationAssignmentsByAgentHumanIdQuery(agentHumanId),
            cancellationToken);
        return Ok(assignments.ToResponse());
    }

    [HttpPut("{chatConversationId:guid}")]
    [EndpointSummary("Actualizar una asignación de conversación")]
    [EndpointDescription("Actualiza el agente humano y las fechas de asignación o desasignación.")]
    [ProducesResponseType(typeof(ChatConversationAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAssignmentResponseDto>> Update(
        Guid chatConversationId,
        [FromBody] UpdateChatConversationAssignmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await sender.Send(dto.ToCommand(chatConversationId), cancellationToken);
            return Ok(assignment.ToResponse());
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

    [HttpDelete("{chatConversationId:guid}")]
    [EndpointSummary("Eliminar una asignación de conversación")]
    [EndpointDescription("Elimina la asignación asociada a la conversación indicada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid chatConversationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatConversationAssignmentCommand(chatConversationId), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
