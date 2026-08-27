using Api.AgentHumans.Dtos;
using Api.AgentHumans.Mappings;
using Application.AgentHumans.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.AgentHumans.Controllers;

[ApiController]
[Route("api/chat/agent-humans")]
public sealed class AgentHumanController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un agente humano")]
    [EndpointDescription("Registra un agente humano para un usuario existente. Un usuario puede tener varios registros de agente. Queda activo por defecto.")]
    [ProducesResponseType(typeof(AgentHumanResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentHumanResponseDto>> Create(
        [FromBody] CreateAgentHumanDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = agent.ToResponse();
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
    [EndpointSummary("Listar agentes humanos")]
    [EndpointDescription("Devuelve todos los agentes humanos registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AgentHumanResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AgentHumanResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var agents = await sender.Send(new GetAllAgentHumansQuery(), cancellationToken);
        return Ok(agents.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener un agente humano por su identificador")]
    [EndpointDescription("Devuelve el agente humano indicado.")]
    [ProducesResponseType(typeof(AgentHumanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentHumanResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var agent = await sender.Send(new GetAgentHumanByIdQuery(id), cancellationToken);
        return agent is null
            ? NotFound(new { Message = $"No se encontró el agente humano '{id}'." })
            : Ok(agent.ToResponse());
    }

    [HttpGet("by-user/{userId:guid}")]
    [EndpointSummary("Consultar agentes humanos por usuario")]
    [EndpointDescription("Devuelve todos los agentes humanos asociados al usuario indicado. Si no hay registros, responde con una lista vacía.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AgentHumanResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AgentHumanResponseDto>>> GetByUserId(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var agents = await sender.Send(new GetAgentHumansByUserIdQuery(userId), cancellationToken);
        return Ok(agents.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar un agente humano")]
    [EndpointDescription("Verifica que el agente exista. El usuario asociado no se modifica; el estado se cambia con activar o desactivar.")]
    [ProducesResponseType(typeof(AgentHumanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentHumanResponseDto>> Update(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = await sender.Send(new UpdateAgentHumanCommand(id), cancellationToken);
            return Ok(agent.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activate")]
    [EndpointSummary("Activar un agente humano")]
    [EndpointDescription("Marca el agente humano como activo.")]
    [ProducesResponseType(typeof(AgentHumanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentHumanResponseDto>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = await sender.Send(new ActivateAgentHumanCommand(id), cancellationToken);
            return Ok(agent.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [EndpointSummary("Desactivar un agente humano")]
    [EndpointDescription("Marca el agente humano como inactivo.")]
    [ProducesResponseType(typeof(AgentHumanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentHumanResponseDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = await sender.Send(new DeactivateAgentHumanCommand(id), cancellationToken);
            return Ok(agent.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
