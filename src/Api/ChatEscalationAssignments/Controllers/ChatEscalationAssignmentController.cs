using Api.ChatEscalationAssignments.Dtos;
using Api.ChatEscalationAssignments.Mappings;
using Api.Common.Security;
using Application.ChatEscalationAssignments.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatEscalationAssignments.Controllers;

[ApiController]
[Route("api/chat/escalation-assignments")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatEscalationAssignmentController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear asignación de escalamiento")]
    [EndpointDescription("Asigna un agente humano a un escalamiento existente.")]
    [ProducesResponseType(typeof(ChatEscalationAssignmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationAssignmentResponseDto>> Create(
        [FromBody] CreateChatEscalationAssignmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = assignment.ToResponse();
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
    [EndpointSummary("Listar asignaciones de escalamiento")]
    [EndpointDescription("Devuelve todas las asignaciones registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationAssignmentResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var assignments = await sender.Send(new GetAllChatEscalationAssignmentsQuery(), cancellationToken);
        return Ok(assignments.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener asignación por identificador")]
    [EndpointDescription("Devuelve la asignación indicada.")]
    [ProducesResponseType(typeof(ChatEscalationAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationAssignmentResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await sender.Send(new GetChatEscalationAssignmentByIdQuery(id), cancellationToken);
        return assignment is null
            ? NotFound(new { Message = $"No se encontró la asignación '{id}'." })
            : Ok(assignment.ToResponse());
    }

    [HttpGet("by-escalation/{chatEscalationId:guid}")]
    [EndpointSummary("Consultar asignaciones por escalamiento")]
    [EndpointDescription("Devuelve las asignaciones asociadas al escalamiento indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationAssignmentResponseDto>>> GetByChatEscalationId(
        Guid chatEscalationId,
        CancellationToken cancellationToken)
    {
        var assignments = await sender.Send(
            new GetChatEscalationAssignmentsByChatEscalationIdQuery(chatEscalationId),
            cancellationToken);
        return Ok(assignments.ToResponse());
    }

    [HttpGet("by-agent/{agentHumanId:guid}")]
    [EndpointSummary("Consultar asignaciones por agente humano")]
    [EndpointDescription("Devuelve las asignaciones asociadas al agente humano indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationAssignmentResponseDto>>> GetByAgentHumanId(
        Guid agentHumanId,
        CancellationToken cancellationToken)
    {
        var assignments = await sender.Send(
            new GetChatEscalationAssignmentsByAgentHumanIdQuery(agentHumanId),
            cancellationToken);
        return Ok(assignments.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar asignación de escalamiento")]
    [EndpointDescription("Actualiza el agente humano y la fecha de asignación.")]
    [ProducesResponseType(typeof(ChatEscalationAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationAssignmentResponseDto>> Update(
        Guid id,
        [FromBody] UpdateChatEscalationAssignmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await sender.Send(dto.ToCommand(id), cancellationToken);
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

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Eliminar asignación de escalamiento")]
    [EndpointDescription("Elimina la asignación indicada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatEscalationAssignmentCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
