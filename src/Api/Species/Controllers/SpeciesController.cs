using Api.Species.Dtos;
using Api.Species.Mappings;
using Application.Species.Abstraction;
using Domain.Species.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Species.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpeciesController : ControllerBase
{
    private readonly ISpeciesRepository _repository;

    public SpeciesController(ISpeciesRepository repository)
    {
        _repository = repository;
    }

    // 1. OBTENER TODAS: GET /api/species
    [HttpGet]
    [EndpointSummary("Obtiene todas las especies")]
    [EndpointDescription("Retorna una lista con todas las especies registradas en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SpeciesResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SpeciesResponseDto>>> GetAll(CancellationToken ct)
    {
        var species = await _repository.GetAllAsync(ct);
        return Ok(species.Select(s => s.ToDto()).ToList());
    }

    // 2. OBTENER POR ID: GET /api/species/{id}
    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene una especie por su ID")]
    [EndpointDescription("Retorna los datos de una especie específica.")]
    [ProducesResponseType(typeof(SpeciesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpeciesResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var species = await _repository.GetByIdAsync(id, ct);
        
        if (species is null)
            return NotFound(new { message = "Especie no encontrada." });

        return Ok(species.ToDto());
    }

    // 3. CREAR ESPECIE: POST /api/species
    [HttpPost]
    [EndpointSummary("Crea una nueva especie")]
    [EndpointDescription("Registra una nueva especie en el sistema.")]
    [ProducesResponseType(typeof(SpeciesResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpeciesResponseDto>> Create([FromBody] CreateSpeciesDto dto, CancellationToken ct)
    {
        var newSpecies = new SpeciesEntity(dto.Name);

        await _repository.AddAsync(newSpecies, ct);

        return CreatedAtAction(nameof(GetById), new { id = newSpecies.Id }, newSpecies.ToDto());
    }

    // 4. ACTUALIZAR ESPECIE: PUT /api/species/{id}
    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una especie existente")]
    [EndpointDescription("Modifica los datos de una especie existente mediante su ID.")]
    [ProducesResponseType(typeof(SpeciesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpeciesResponseDto>> Update(Guid id, [FromBody] UpdateSpeciesDto dto, CancellationToken ct)
    {
        var species = await _repository.GetByIdAsync(id, ct);

        if (species is null)
            return NotFound(new { message = "Especie no encontrada." });

        species.Update(dto.Name);
        await _repository.UpdateAsync(species, ct);

        return Ok(species.ToDto());
    }

    // 5. ELIMINAR ESPECIE: DELETE /api/species/{id}
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina una especie")]
    [EndpointDescription("Elimina permanentemente una especie del sistema por su ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var species = await _repository.GetByIdAsync(id, ct);

        if (species is null)
            return NotFound(new { message = "Especie no encontrada." });

        await _repository.DeleteAsync(species, ct);
        return NoContent();
    }
}
