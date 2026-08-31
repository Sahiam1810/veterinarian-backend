using Api.ChatEscalationResolutions.Dtos;
using Api.ChatEscalationResolutions.Mappings;
using Api.Common.Security;
using Application.ChatEscalationResolutions.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatEscalationResolutions.Controllers;

[ApiController]
[Route("api/chat/escalation-resolutions")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatEscalationResolutionController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear resolución de escalamiento")]
    [EndpointDescription("Registra la resolución de un escalamiento existente.")]
    [ProducesResponseType(typeof(ChatEscalationResolutionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationResolutionResponseDto>> Create(
        [FromBody] CreateChatEscalationResolutionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var resolution = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = resolution.ToResponse();
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
    [EndpointSummary("Listar resoluciones de escalamiento")]
    [EndpointDescription("Devuelve todas las resoluciones registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationResolutionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationResolutionResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var resolutions = await sender.Send(new GetAllChatEscalationResolutionsQuery(), cancellationToken);
        return Ok(resolutions.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener resolución por identificador")]
    [EndpointDescription("Devuelve la resolución indicada.")]
    [ProducesResponseType(typeof(ChatEscalationResolutionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationResolutionResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resolution = await sender.Send(new GetChatEscalationResolutionByIdQuery(id), cancellationToken);
        return resolution is null
            ? NotFound(new { Message = $"No se encontró la resolución '{id}'." })
            : Ok(resolution.ToResponse());
    }

    [HttpGet("by-escalation/{chatEscalationId:guid}")]
    [EndpointSummary("Consultar resoluciones por escalamiento")]
    [EndpointDescription("Devuelve las resoluciones asociadas al escalamiento indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatEscalationResolutionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatEscalationResolutionResponseDto>>> GetByChatEscalationId(
        Guid chatEscalationId,
        CancellationToken cancellationToken)
    {
        var resolutions = await sender.Send(
            new GetChatEscalationResolutionsByChatEscalationIdQuery(chatEscalationId),
            cancellationToken);
        return Ok(resolutions.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar resolución de escalamiento")]
    [EndpointDescription("Actualiza los datos de la resolución.")]
    [ProducesResponseType(typeof(ChatEscalationResolutionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatEscalationResolutionResponseDto>> Update(
        Guid id,
        [FromBody] UpdateChatEscalationResolutionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var resolution = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(resolution.ToResponse());
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
    [EndpointSummary("Eliminar resolución de escalamiento")]
    [EndpointDescription("Elimina la resolución indicada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatEscalationResolutionCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
