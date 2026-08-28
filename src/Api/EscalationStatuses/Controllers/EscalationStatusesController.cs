using Api.Common.Security;
using Api.EscalationStatuses.Dtos;
using Api.EscalationStatuses.Mappings;
using Application.EscalationStatuses.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.EscalationStatuses.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class EscalationStatusesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los estados de escalamiento")]
    [EndpointDescription("Lista los estados registrados para escalamientos.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EscalationStatusResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EscalationStatusResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllEscalationStatusesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un estado de escalamiento")]
    [EndpointDescription("Busca un estado de escalamiento por su identificador.")]
    [ProducesResponseType(typeof(EscalationStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EscalationStatusResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetEscalationStatusByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea un estado de escalamiento")]
    [EndpointDescription("Registra un nuevo estado de escalamiento.")]
    [ProducesResponseType(typeof(EscalationStatusResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EscalationStatusResponseDto>> Create(CreateEscalationStatusDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateEscalationStatusCommand(dto.Name), ct);
        var escalationStatus = await sender.Send(new GetEscalationStatusByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, escalationStatus.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un estado de escalamiento")]
    [EndpointDescription("Actualiza el nombre de un estado de escalamiento.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateEscalationStatusDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateEscalationStatusCommand(id, dto.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un estado de escalamiento")]
    [EndpointDescription("Elimina un estado de escalamiento por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteEscalationStatusCommand(id), ct);
        return NoContent();
    }
}
