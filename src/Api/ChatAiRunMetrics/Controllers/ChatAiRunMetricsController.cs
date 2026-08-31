using Api.ChatAiRunMetrics.Dtos;
using Api.ChatAiRunMetrics.Mappings;
using Api.Common.Security;
using Application.ChatAiRunMetrics.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatAiRunMetrics.Controllers;

[ApiController]
[Route("api/chat/ai-run-metrics")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatAiRunMetricsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registrar métricas de una ejecución de IA")]
    [EndpointDescription("Persiste tokens y costo para una ejecución de IA existente.")]
    [ProducesResponseType(typeof(ChatAiRunMetricsResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChatAiRunMetricsResponseDto>> Create(
        [FromBody] CreateChatAiRunMetricsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = metrics.ToResponse();
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
        catch (ConflictException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener métricas por identificador")]
    [EndpointDescription("Devuelve el registro de métricas indicado.")]
    [ProducesResponseType(typeof(ChatAiRunMetricsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunMetricsResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var metrics = await sender.Send(new GetChatAiRunMetricsByIdQuery(id), cancellationToken);
        return metrics is null
            ? NotFound(new { Message = $"No se encontraron métricas '{id}'." })
            : Ok(metrics.ToResponse());
    }

    [HttpGet("run/{chatAiRunId:guid}")]
    [EndpointSummary("Obtener métricas por ejecución de IA")]
    [EndpointDescription("Devuelve las métricas asociadas a la ejecución de IA indicada.")]
    [ProducesResponseType(typeof(ChatAiRunMetricsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunMetricsResponseDto>> GetByChatAiRunId(
        Guid chatAiRunId,
        CancellationToken cancellationToken)
    {
        var metrics = await sender.Send(
            new GetChatAiRunMetricsByChatAiRunIdQuery(chatAiRunId),
            cancellationToken);
        return metrics is null
            ? NotFound(new { Message = $"No se encontraron métricas para la ejecución '{chatAiRunId}'." })
            : Ok(metrics.ToResponse());
    }
}
