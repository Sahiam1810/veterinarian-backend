using Api.AiRunStatuses.Dtos;
using Api.AiRunStatuses.Mappings;
using Api.Common.Security;
using Application.AiRunStatuses.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.AiRunStatuses.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AiRunStatusesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los estados de ejecución AI")]
    [EndpointDescription("Lista los estados de ejecución registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AiRunStatusResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AiRunStatusResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllAiRunStatusesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un estado de ejecución AI")]
    [EndpointDescription("Busca un estado por su identificador.")]
    [ProducesResponseType(typeof(AiRunStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiRunStatusResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetAiRunStatusByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea un estado de ejecución AI")]
    [EndpointDescription("Registra un nuevo estado de ejecución.")]
    [ProducesResponseType(typeof(AiRunStatusResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiRunStatusResponseDto>> Create(CreateAiRunStatusDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateAiRunStatusCommand(dto.NameStatus), ct);
        var aiRunStatus = await sender.Send(new GetAiRunStatusByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, aiRunStatus.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un estado de ejecución AI")]
    [EndpointDescription("Actualiza el nombre de un estado de ejecución.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateAiRunStatusDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateAiRunStatusCommand(id, dto.NameStatus), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un estado de ejecución AI")]
    [EndpointDescription("Elimina un estado por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteAiRunStatusCommand(id), ct);
        return NoContent();
    }
}
