using Api.ClientsPets.Dtos;
using Api.ClientsPets.Mappings;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.ClientsPets.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ClientsPets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClientsPetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene las relaciones cliente-mascota")]
    [EndpointDescription("Lista todas las asociaciones entre clientes y mascotas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClientPetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClientPetResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllClientPetsQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene una relación cliente-mascota")]
    [EndpointDescription("Busca una asociación por su identificador.")]
    [ProducesResponseType(typeof(ClientPetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientPetResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetClientPetByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [RequirePermission("Mascotas", PermissionAction.Create)]
    [EndpointSummary("Asocia un cliente a una mascota")]
    [EndpointDescription("Crea una nueva relación entre un cliente y una mascota.")]
    [ProducesResponseType(typeof(ClientPetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientPetResponseDto>> Create(CreateClientPetDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateClientPetCommand(dto.ClientId, dto.PetId, dto.IsPrimaryOwner), ct);
        var clientPet = await sender.Send(new GetClientPetByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, clientPet.ToDto());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Mascotas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una relación cliente-mascota")]
    [EndpointDescription("Actualiza si el cliente es el propietario principal.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateClientPetDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateClientPetCommand(id, dto.IsPrimaryOwner), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Mascotas", PermissionAction.Delete)]
    [EndpointSummary("Elimina una relación cliente-mascota")]
    [EndpointDescription("Elimina una asociación entre cliente y mascota.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteClientPetCommand(id), ct);
        return NoContent();
    }
}
