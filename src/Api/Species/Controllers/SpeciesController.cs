using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Species.Dtos;
using Api.Species.Mappings;
using Application.Species.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Species.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SpeciesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [EndpointSummary("Obtiene todas las especies")]
    [EndpointDescription("Retorna una lista con todas las especies registradas en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SpeciesResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SpeciesResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var species = await sender.Send(
            new GetAllSpeciesQuery(),
            cancellationToken);

        return Ok(species.ToDto());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Especies y Razas", PermissionAction.View)]
    [EndpointSummary("Obtiene una especie por su ID")]
    [EndpointDescription("Retorna los datos de una especie específica.")]
    [ProducesResponseType(typeof(SpeciesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpeciesResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var species = await sender.Send(
            new GetSpeciesByIdQuery(id),
            cancellationToken);

        return Ok(species.ToDto());
    }

    [HttpPost]
    [RequirePermission("Especies y Razas", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva especie")]
    [EndpointDescription("Registra una nueva especie en el sistema.")]
    [ProducesResponseType(typeof(SpeciesResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpeciesResponseDto>> Create(
        [FromBody] CreateSpeciesDto dto,
        CancellationToken cancellationToken)
    {
        var speciesId = await sender.Send(
            dto.ToCommand(),
            cancellationToken);

        var species = await sender.Send(
            new GetSpeciesByIdQuery(speciesId),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = speciesId },
            species.ToDto());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Especies y Razas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una especie existente")]
    [EndpointDescription("Modifica los datos de una especie existente mediante su ID.")]
    [ProducesResponseType(typeof(SpeciesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpeciesResponseDto>> Update(
        Guid id,
        [FromBody] UpdateSpeciesDto dto,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            dto.ToCommand(id),
            cancellationToken);

        var species = await sender.Send(
            new GetSpeciesByIdQuery(id),
            cancellationToken);

        return Ok(species.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Especies y Razas", PermissionAction.Delete)]
    [EndpointSummary("Elimina una especie")]
    [EndpointDescription("Elimina permanentemente una especie del sistema por su ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteSpeciesCommand(id),
            cancellationToken);

        return NoContent();
    }
}
