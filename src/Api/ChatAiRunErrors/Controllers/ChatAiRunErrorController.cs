using Api.ChatAiRunErrors.Dtos;
using Api.ChatAiRunErrors.Mappings;
using Api.Common.Security;
using Application.ChatAiRunErrors.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatAiRunErrors.Controllers;

[ApiController]
[Route("api/chat/ai-run-errors")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatAiRunErrorController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registrar un error de ejecución de IA")]
    [EndpointDescription("Persiste un error asociado a una ejecución de IA existente.")]
    [ProducesResponseType(typeof(ChatAiRunErrorResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunErrorResponseDto>> Create(
        [FromBody] CreateChatAiRunErrorDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = error.ToResponse();
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
    [EndpointSummary("Obtener un error por identificador")]
    [EndpointDescription("Devuelve el error de ejecución indicado.")]
    [ProducesResponseType(typeof(ChatAiRunErrorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatAiRunErrorResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var error = await sender.Send(new GetChatAiRunErrorByIdQuery(id), cancellationToken);
        return error is null
            ? NotFound(new { Message = $"No se encontró el error '{id}'." })
            : Ok(error.ToResponse());
    }

    [HttpGet("run/{chatAiRunId:guid}")]
    [EndpointSummary("Listar errores por ejecución de IA")]
    [EndpointDescription("Devuelve todos los errores asociados a la ejecución de IA indicada.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatAiRunErrorResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatAiRunErrorResponseDto>>> GetByChatAiRunId(
        Guid chatAiRunId,
        CancellationToken cancellationToken)
    {
        var errors = await sender.Send(
            new GetChatAiRunErrorsByChatAiRunIdQuery(chatAiRunId),
            cancellationToken);
        return Ok(errors.ToResponse());
    }
}
