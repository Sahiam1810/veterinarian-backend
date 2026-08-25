using Microsoft.AspNetCore.Mvc;
using Application.Species.Abstraction;
using Api.Species.Dtos;
using Api.Species.Mappings;
using veterinarian_backend.Domain.Species.Entities;

[ApiController]
[Route("api/[controller]")] // La ruta será: /api/species
public class SpeciesController : ControllerBase
{
    private readonly ISpeciesRepository _repository;

    public SpeciesController(ISpeciesRepository repository)
    {
        _repository = repository;
    }

    // 1. OBTENER POR ID: GET /api/species/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpeciesResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var species = await _repository.GetByIdAsync(id, ct);
        
        if (species is null)
            return NotFound(new { message = "Especie no encontrada." }); // 404

        // Mapeas la entidad al DTO de salida
        var response = species.ToDto();
        return Ok(response); // 200 OK
    }

    // 2. CREAR ESPECIE: POST /api/species
    [HttpPost]
    public async Task<ActionResult<SpeciesResponseDto>> Create([FromBody] CreateSpeciesDto dto, CancellationToken ct)
    {
        // Conviertes el DTO de entrada a la Entidad de Dominio
        var newSpecies = dto.ToEntity();

        await _repository.AddAsync(newSpecies, ct);

        var response = newSpecies.ToDto();
        
        return CreatedAtAction(nameof(GetById), new { id = newSpecies.Id }, response);
    }
}
