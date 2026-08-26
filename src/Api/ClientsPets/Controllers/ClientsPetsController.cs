using Api.ClientsPets.Dtos;
using Api.ClientsPets.Mappings;
using Application.ClientsPets.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.ClientsPets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClientsPetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene las relaciones cliente-mascota")]
    [EndpointDescription("Lista todas las asociaciones entre clientes y mascotas.")]
    public async Task<ActionResult<IReadOnlyCollection<ClientPetResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllClientPetsQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene una relación cliente-mascota")]
    [EndpointDescription("Busca una asociación por su identificador.")]
    public async Task<ActionResult<ClientPetResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetClientPetByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Asocia un cliente a una mascota")]
    [EndpointDescription("Crea una nueva relación entre un cliente y una mascota.")]
    public async Task<ActionResult<ClientPetResponseDto>> Create(CreateClientPetDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateClientPetCommand(dto.ClientId, dto.PetId, dto.IsPrimaryOwner), ct);
        var clientPet = await sender.Send(new GetClientPetByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, clientPet.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una relación cliente-mascota")]
    [EndpointDescription("Actualiza si el cliente es el propietario principal.")]
    public async Task<IActionResult> Update(Guid id, UpdateClientPetDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateClientPetCommand(id, dto.IsPrimaryOwner), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina una relación cliente-mascota")]
    [EndpointDescription("Elimina una asociación entre cliente y mascota.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteClientPetCommand(id), ct);
        return NoContent();
    }
}
