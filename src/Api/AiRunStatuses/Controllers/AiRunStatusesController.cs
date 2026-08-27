using Api.AiRunStatuses.Dtos;
using Api.AiRunStatuses.Mappings;
using Application.AiRunStatuses.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.AiRunStatuses.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AiRunStatusesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los estados de ejecución AI")]
    [EndpointDescription("Lista los estados de ejecución registrados.")]
    public async Task<ActionResult<IReadOnlyCollection<AiRunStatusResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllAiRunStatusesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un estado de ejecución AI")]
    [EndpointDescription("Busca un estado por su identificador.")]
    public async Task<ActionResult<AiRunStatusResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetAiRunStatusByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea un estado de ejecución AI")]
    [EndpointDescription("Registra un nuevo estado de ejecución.")]
    public async Task<ActionResult<AiRunStatusResponseDto>> Create(CreateAiRunStatusDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateAiRunStatusCommand(dto.NameStatus), ct);
        var aiRunStatus = await sender.Send(new GetAiRunStatusByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, aiRunStatus.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un estado de ejecución AI")]
    [EndpointDescription("Actualiza el nombre de un estado de ejecución.")]
    public async Task<IActionResult> Update(Guid id, UpdateAiRunStatusDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateAiRunStatusCommand(id, dto.NameStatus), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un estado de ejecución AI")]
    [EndpointDescription("Elimina un estado por su identificador.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteAiRunStatusCommand(id), ct);
        return NoContent();
    }
}
