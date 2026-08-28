using Api.Common.Security;
using Api.Priorities.Dtos;
using Api.Priorities.Mappings;
using Application.Priorities.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Priorities.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrioritiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene las prioridades")]
    [EndpointDescription("Lista las prioridades registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PriorityResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PriorityResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllPrioritiesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene una prioridad")]
    [EndpointDescription("Busca una prioridad por su identificador.")]
    [ProducesResponseType(typeof(PriorityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriorityResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetPriorityByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Crea una prioridad")]
    [EndpointDescription("Registra una nueva prioridad.")]
    [ProducesResponseType(typeof(PriorityResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriorityResponseDto>> Create(CreatePriorityDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreatePriorityCommand(dto.Name), ct);
        var priority = await sender.Send(new GetPriorityByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, priority.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Actualiza una prioridad")]
    [EndpointDescription("Actualiza el nombre de una prioridad.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdatePriorityDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdatePriorityCommand(id, dto.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Elimina una prioridad")]
    [EndpointDescription("Elimina una prioridad por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeletePriorityCommand(id), ct);
        return NoContent();
    }
}
