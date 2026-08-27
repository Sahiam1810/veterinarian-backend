using Api.Common.Security;
using Api.MessageTypes.Dtos;
using Api.MessageTypes.Mappings;
using Application.MessageTypes.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.MessageTypes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class MessageTypesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los tipos de mensaje")]
    [EndpointDescription("Lista los tipos de mensaje registrados.")]
    public async Task<ActionResult<IReadOnlyCollection<MessageTypeResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllMessageTypesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un tipo de mensaje")]
    [EndpointDescription("Busca un tipo de mensaje por su identificador.")]
    public async Task<ActionResult<MessageTypeResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetMessageTypeByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea un tipo de mensaje")]
    [EndpointDescription("Registra un nuevo tipo de mensaje.")]
    public async Task<ActionResult<MessageTypeResponseDto>> Create(CreateMessageTypeDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateMessageTypeCommand(dto.Name), ct);
        var messageType = await sender.Send(new GetMessageTypeByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, messageType.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un tipo de mensaje")]
    [EndpointDescription("Actualiza el nombre de un tipo de mensaje.")]
    public async Task<IActionResult> Update(Guid id, UpdateMessageTypeDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateMessageTypeCommand(id, dto.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un tipo de mensaje")]
    [EndpointDescription("Elimina un tipo de mensaje por su identificador.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteMessageTypeCommand(id), ct);
        return NoContent();
    }
}
