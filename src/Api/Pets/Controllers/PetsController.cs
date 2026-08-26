using Api.Pets.Dtos;
using Api.Pets.Mappings;
using Application.Pets.UseCases;
using MediatR;
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

    // GET /api/pets
    [HttpGet]
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
