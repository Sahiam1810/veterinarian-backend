using System.Security.Claims;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Pets.Dtos;
using Api.Pets.Mappings;
using Application.Pets.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Pets.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/pets/mine
    [HttpGet("mine")]
    [Authorize(Policy = AuthorizationPolicies.ClientOnly)]
    [EndpointSummary("Obtiene las mascotas del cliente autenticado")]
    [EndpointDescription("Retorna las mascotas asociadas al cliente correspondiente al usuario autenticado actual (portal de dueño).")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OwnedPetProfileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<OwnedPetProfileResponseDto>>> GetMine(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var pets = await _mediator.Send(new GetMyPetsQuery(userAccountId), ct);
        return Ok(pets.Select(p => p.ToDto()).ToList());
    }

    [HttpPatch("mine/{petId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ClientOnly)]
    [EndpointSummary("Actualiza parcialmente una mascota del cliente autenticado")]
    [EndpointDescription("Solo permite modificar mascotas vinculadas al cliente del JWT y exige la versión consultada.")]
    [ProducesResponseType(typeof(OwnedPetProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnedPetProfileResponseDto>> UpdateMine(
        Guid petId,
        [FromBody] UpdateOwnedPetProfileDto dto,
        CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userAccountId))
            return Unauthorized();

        var profile = await _mediator.Send(new UpdateMyPetProfileCommand(
            userAccountId,
            petId,
            dto.Name,
            dto.Age,
            dto.Gender,
            dto.Weight,
            dto.Observations,
            dto.ChangeObservations,
            dto.SpeciesId,
            dto.RaceId,
            dto.ExpectedUpdatedAt), ct);
        return Ok(profile.ToDto());
    }

    // GET /api/pets
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene todas las mascotas")]
    [EndpointDescription("Retorna una lista de todas las mascotas registradas en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PetResponseDto>>> GetAll(CancellationToken ct)
    {
        var pets = await _mediator.Send(new GetAllPetsQuery(), ct);
        return Ok(pets.Select(p => p.ToDto()).ToList());
    }

    // GET /api/pets/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene una mascota por su ID")]
    [EndpointDescription("Retorna los detalles de una mascota específica buscando por su identificador único.")]
    [ProducesResponseType(typeof(PetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var pet = await _mediator.Send(new GetPetByIdQuery(id), ct);
        return Ok(pet.ToDto());
    }

    // POST /api/pets
    [HttpPost]
    [RequirePermission("Mascotas", PermissionAction.Create)]
    [EndpointSummary("Registra una nueva mascota")]
    [EndpointDescription("Crea un nuevo registro de mascota asociándolo a una especie y raza existentes.")]
    [ProducesResponseType(typeof(PetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponseDto>> Create([FromBody] CreatePetDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreatePetCommand(
            dto.Name,
            dto.Age,
            dto.Gender,
            dto.Weight,
            dto.Observations,
            dto.SpeciesId,
            dto.RaceId), ct);

        var pet = await _mediator.Send(new GetPetByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, pet.ToDto());
    }

    // PUT /api/pets/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("Mascotas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza los datos de una mascota")]
    [EndpointDescription("Modifica los datos de una mascota existente identificada por su ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePetDto dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdatePetCommand(
            id,
            dto.Name,
            dto.Age,
            dto.Gender,
            dto.Weight,
            dto.Observations,
            dto.SpeciesId,
            dto.RaceId), ct);

        return NoContent();
    }

    // DELETE /api/pets/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("Mascotas", PermissionAction.Delete)]
    [EndpointSummary("Elimina una mascota")]
    [EndpointDescription("Elimina permanentemente el registro de una mascota del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeletePetCommand(id), ct);
        return NoContent();
    }
}
