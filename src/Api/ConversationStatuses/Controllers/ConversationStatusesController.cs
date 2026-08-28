using Api.Common.Security;
using Api.ConversationStatuses.Dtos;
using Api.ConversationStatuses.Mappings;
using Application.ConversationStatuses.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ConversationStatuses.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ConversationStatusesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los estados de conversación")]
    [EndpointDescription("Lista los estados registrados para las conversaciones.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConversationStatusResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ConversationStatusResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllConversationStatusesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un estado de conversación")]
    [EndpointDescription("Busca un estado de conversación por su identificador.")]
    [ProducesResponseType(typeof(ConversationStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationStatusResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetConversationStatusByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea un estado de conversación")]
    [EndpointDescription("Registra un nuevo estado de conversación.")]
    [ProducesResponseType(typeof(ConversationStatusResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationStatusResponseDto>> Create(CreateConversationStatusDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateConversationStatusCommand(dto.Name), ct);
        var conversationStatus = await sender.Send(new GetConversationStatusByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, conversationStatus.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un estado de conversación")]
    [EndpointDescription("Actualiza el nombre de un estado de conversación.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateConversationStatusDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateConversationStatusCommand(id, dto.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un estado de conversación")]
    [EndpointDescription("Elimina un estado de conversación por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteConversationStatusCommand(id), ct);
        return NoContent();
    }
}
