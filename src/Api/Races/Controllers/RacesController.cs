using Microsoft.AspNetCore.Mvc;
using Application.Races.Abstraction;
using Api.Races.Dtos;
using veterinarian_backend.Domain.Races.Entities;


[ApiController]
[Route("api/[controller]")] // La ruta será: /api/races
public class RacesController : ControllerBase
{
    private readonly IRaceRepository _repository;

    public RacesController(IRaceRepository repository)
    {
        _repository = repository;
    }

    // 1. OBTENER POR ID: GET /api/races/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RaceResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var race = await _repository.GetByIdAsync(id, ct);
        
        if (race is null)
            return NotFound(new { message = "Raza no encontrada." }); // 404

        // Mapeas la entidad al DTO de salida
        var response = new RaceResponseDto(race.Id, race.Name);
        return Ok(response); // 200 OK
    }

    // 2. CREAR RAZA: POST /api/races
    [HttpPost]
    public async Task<ActionResult<RaceResponseDto>> Create([FromBody] CreateRaceDto dto, CancellationToken ct)
    {
        var newRace = new RaceEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };

        await _repository.AddAsync(newRace, ct);

        var response = new RaceResponseDto(newRace.Id, newRace.Name);
        return CreatedAtAction(nameof(GetById), new { id = newRace.Id }, response);
    }

    // 3. ACTUALIZAR RAZA: PUT /api/races/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RaceResponseDto>> Update(Guid id, [FromBody] UpdateRaceDto dto, CancellationToken ct)
    {
        var race = await _repository.GetByIdAsync(id, ct);

        if (race is null)
            return NotFound(new { message = "Raza no encontrada." });

        race.Name = dto.Name;
        await _repository.UpdateAsync(race, ct);

        var response = new RaceResponseDto(race.Id, race.Name);
        return Ok(response);
    }

    // 4. ELIMINAR RAZA: DELETE /api/races/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var race = await _repository.GetByIdAsync(id, ct);

        if (race is null)
            return NotFound(new { message = "Raza no encontrada." });

        await _repository.DeleteAsync(race, ct);
        return NoContent();
    }
}