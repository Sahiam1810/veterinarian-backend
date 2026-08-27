using Api.Common.Security;
using Api.Races.Dtos;
using Api.Races.Mappings;
using Application.Races.Abstraction;
using Domain.Races.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Races.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RacesController : ControllerBase
{
    private readonly IRaceRepository _repository;

    public RacesController(IRaceRepository repository)
    {
        _repository = repository;
    }

    // 1. OBTENER TODAS: GET /api/races
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene todas las razas")]
    [EndpointDescription("Retorna una lista con todas las razas registradas en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RaceResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RaceResponseDto>>> GetAll(CancellationToken ct)
    {
        var races = await _repository.GetAllAsync(ct);
        return Ok(races.Select(r => r.ToDto()).ToList());
    }

    // 2. OBTENER POR ID: GET /api/races/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene una raza por su ID")]
    [EndpointDescription("Retorna los datos de una raza específica.")]
    [ProducesResponseType(typeof(RaceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var race = await _repository.GetByIdAsync(id, ct);
        
        if (race is null)
            return NotFound(new { message = "Raza no encontrada." });

        return Ok(race.ToDto());
    }

    // 3. CREAR RAZA: POST /api/races
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Crea una nueva raza")]
    [EndpointDescription("Registra una nueva raza en el sistema.")]
    [ProducesResponseType(typeof(RaceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceResponseDto>> Create([FromBody] CreateRaceDto dto, CancellationToken ct)
    {
        var newRace = new RaceEntity(dto.Name);

        await _repository.AddAsync(newRace, ct);

        return CreatedAtAction(nameof(GetById), new { id = newRace.Id }, newRace.ToDto());
    }

    // 4. ACTUALIZAR RAZA: PUT /api/races/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Actualiza una raza existente")]
    [EndpointDescription("Modifica los datos de una raza existente mediante su ID.")]
    [ProducesResponseType(typeof(RaceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceResponseDto>> Update(Guid id, [FromBody] UpdateRaceDto dto, CancellationToken ct)
    {
        var race = await _repository.GetByIdAsync(id, ct);

        if (race is null)
            return NotFound(new { message = "Raza no encontrada." });

        race.Update(dto.Name);
        await _repository.UpdateAsync(race, ct);

        return Ok(race.ToDto());
    }

    // 5. ELIMINAR RAZA: DELETE /api/races/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Elimina una raza")]
    [EndpointDescription("Elimina permanentemente una raza del sistema por su ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var race = await _repository.GetByIdAsync(id, ct);

        if (race is null)
            return NotFound(new { message = "Raza no encontrada." });

        await _repository.DeleteAsync(race, ct);
        return NoContent();
    }
}