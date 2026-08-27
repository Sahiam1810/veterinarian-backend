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
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class PrioritiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene las prioridades")]
    [EndpointDescription("Lista las prioridades registradas.")]
    public async Task<ActionResult<IReadOnlyCollection<PriorityResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllPrioritiesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene una prioridad")]
    [EndpointDescription("Busca una prioridad por su identificador.")]
    public async Task<ActionResult<PriorityResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetPriorityByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea una prioridad")]
    [EndpointDescription("Registra una nueva prioridad.")]
    public async Task<ActionResult<PriorityResponseDto>> Create(CreatePriorityDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreatePriorityCommand(dto.Name), ct);
        var priority = await sender.Send(new GetPriorityByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, priority.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una prioridad")]
    [EndpointDescription("Actualiza el nombre de una prioridad.")]
    public async Task<IActionResult> Update(Guid id, UpdatePriorityDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdatePriorityCommand(id, dto.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina una prioridad")]
    [EndpointDescription("Elimina una prioridad por su identificador.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeletePriorityCommand(id), ct);
        return NoContent();
    }
}
