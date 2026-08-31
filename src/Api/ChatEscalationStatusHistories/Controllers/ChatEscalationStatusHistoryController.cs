using Api.ChatEscalationStatusHistories.Dtos;
using Api.ChatEscalationStatusHistories.Mappings;
using Api.Common.Security;
using Application.ChatEscalationStatusHistories.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatEscalationStatusHistories.Controllers;

[ApiController]
[Route("api/chat/escalation-status-histories")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatEscalationStatusHistoryController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear historial de estado de escalamiento")]
    [EndpointDescription("Registra un cambio de estado para un escalamiento existente.")]
    [ProducesResponseType(typeof(ChatEscalationStatusHistoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationStatusHistoryResponseDto>> Create(
        [FromBody] CreateChatEscalationStatusHistoryDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = history.ToResponse();
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
    [EndpointSummary("Listar historiales de estado de escalamiento")]
    [EndpointDescription("Devuelve todos los historiales registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationStatusHistoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationStatusHistoryResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var histories = await sender.Send(new GetAllChatEscalationStatusHistoriesQuery(), cancellationToken);
        return Ok(histories.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener historial por identificador")]
    [EndpointDescription("Devuelve el historial de estado indicado.")]
    [ProducesResponseType(typeof(ChatEscalationStatusHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationStatusHistoryResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await sender.Send(new GetChatEscalationStatusHistoryByIdQuery(id), cancellationToken);
        return history is null
            ? NotFound(new { Message = $"No se encontró el historial '{id}'." })
            : Ok(history.ToResponse());
    }

    [HttpGet("by-escalation/{chatEscalationId:guid}")]
    [EndpointSummary("Consultar historiales por escalamiento")]
    [EndpointDescription("Devuelve los historiales asociados al escalamiento indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationStatusHistoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationStatusHistoryResponseDto>>> GetByChatEscalationId(
        Guid chatEscalationId,
        CancellationToken cancellationToken)
    {
        var histories = await sender.Send(
            new GetChatEscalationStatusHistoriesByChatEscalationIdQuery(chatEscalationId),
            cancellationToken);
        return Ok(histories.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar historial de estado de escalamiento")]
    [EndpointDescription("Actualiza el estado registrado en el historial.")]
    [ProducesResponseType(typeof(ChatEscalationStatusHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationStatusHistoryResponseDto>> Update(
        Guid id,
        [FromBody] UpdateChatEscalationStatusHistoryDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(history.ToResponse());
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
    [EndpointSummary("Eliminar historial de estado de escalamiento")]
    [EndpointDescription("Elimina el historial indicado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatEscalationStatusHistoryCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
