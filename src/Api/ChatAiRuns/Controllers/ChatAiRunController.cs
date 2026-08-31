using Api.ChatAiRuns.Dtos;
using Api.ChatAiRuns.Mappings;
using Api.Common.Security;
using Application.ChatAiRuns.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatAiRuns.Controllers;

[ApiController]
[Route("api/chat/ai-runs")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatAiRunController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear una ejecución de IA de chat")]
    [EndpointDescription("Registra la cabecera de una ejecución de IA asociada a un mensaje.")]
    [ProducesResponseType(typeof(ChatAiRunResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunResponseDto>> Create(
        [FromBody] CreateChatAiRunDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = run.ToResponse();
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

    [HttpPatch("{id:guid}/status")]
    [EndpointSummary("Actualizar el estado de una ejecución de IA")]
    [EndpointDescription("Cambia el estado de ejecución validando que el catálogo exista.")]
    [ProducesResponseType(typeof(ChatAiRunResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunResponseDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateChatAiRunStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(run.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener una ejecución de IA por su identificador")]
    [EndpointDescription("Devuelve la ejecución de IA indicada.")]
    [ProducesResponseType(typeof(ChatAiRunResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var run = await sender.Send(new GetChatAiRunByIdQuery(id), cancellationToken);
        return run is null
            ? NotFound(new { Message = $"No se encontró la ejecución de IA '{id}'." })
            : Ok(run.ToResponse());
    }

    [HttpGet("conversation/{chatConversationId:guid}")]
    [EndpointSummary("Listar ejecuciones de IA por conversación")]
    [EndpointDescription("Devuelve todas las ejecuciones de IA asociadas a la conversación indicada.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatAiRunResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatAiRunResponseDto>>> GetByConversationId(
        Guid chatConversationId,
        CancellationToken cancellationToken)
    {
        var runs = await sender.Send(
            new GetChatAiRunsByConversationIdQuery(chatConversationId),
            cancellationToken);
        return Ok(runs.ToResponse());
    }
}
