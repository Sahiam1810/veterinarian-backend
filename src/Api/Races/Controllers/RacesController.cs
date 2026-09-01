using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Races.Dtos;
using Api.Races.Mappings;
using Application.Races.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Races.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RacesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Especies y Razas", PermissionAction.View)]
    [EndpointSummary("Obtiene todas las razas")]
    [EndpointDescription("Retorna una lista con todas las razas registradas en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RaceResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RaceResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var races = await sender.Send(
            new GetAllRacesQuery(),
            cancellationToken);

        return Ok(races.ToDto());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Especies y Razas", PermissionAction.View)]
    [EndpointSummary("Obtiene una raza por su ID")]
    [EndpointDescription("Retorna los datos de una raza específica.")]
    [ProducesResponseType(typeof(RaceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var race = await sender.Send(
            new GetRaceByIdQuery(id),
            cancellationToken);

        return Ok(race.ToDto());
    }

    [HttpPost]
    [RequirePermission("Especies y Razas", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva raza")]
    [EndpointDescription("Registra una nueva raza en el sistema.")]
    [ProducesResponseType(typeof(RaceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RaceResponseDto>> Create(
        [FromBody] CreateRaceDto dto,
        CancellationToken cancellationToken)
    {
        var raceId = await sender.Send(
            dto.ToCommand(),
            cancellationToken);

        var race = await sender.Send(
            new GetRaceByIdQuery(raceId),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = raceId },
            race.ToDto());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Especies y Razas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una raza existente")]
    [EndpointDescription("Modifica los datos de una raza existente mediante su ID.")]
    [ProducesResponseType(typeof(RaceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RaceResponseDto>> Update(
        Guid id,
        [FromBody] UpdateRaceDto dto,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            dto.ToCommand(id),
            cancellationToken);

        var race = await sender.Send(
            new GetRaceByIdQuery(id),
            cancellationToken);

        return Ok(race.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Especies y Razas", PermissionAction.Delete)]
    [EndpointSummary("Elimina una raza")]
    [EndpointDescription("Elimina permanentemente una raza del sistema por su ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteRaceCommand(id),
            cancellationToken);

        return NoContent();
    }
}
