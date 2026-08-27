using Api.Common.Security;
using Api.SenderTypes.Dtos;
using Api.SenderTypes.Mappings;
using Application.SenderTypes.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.SenderTypes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class SenderTypesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los tipos de remitente")]
    [EndpointDescription("Lista los tipos de mensaje registrados.")]
    public async Task<ActionResult<IReadOnlyCollection<SenderTypeResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllSenderTypesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un tipo de remitente")]
    [EndpointDescription("Busca un tipo de mensaje por su identificador.")]
    public async Task<ActionResult<SenderTypeResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetSenderTypeByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea un tipo de remitente")]
    [EndpointDescription("Registra un tipo de mensaje.")]
    public async Task<ActionResult<SenderTypeResponseDto>> Create(CreateSenderTypeDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSenderTypeCommand(dto.Name), ct);
        var senderType = await sender.Send(new GetSenderTypeByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, senderType.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un tipo de remitente")]
    [EndpointDescription("Actualiza el nombre de un tipo de mensaje.")]
    public async Task<IActionResult> Update(Guid id, UpdateSenderTypeDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateSenderTypeCommand(id, dto.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un tipo de remitente")]
    [EndpointDescription("Elimina un tipo de mensaje por su identificador.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSenderTypeCommand(id), ct);
        return NoContent();
    }
}
