using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Specialties.Dtos;
using Api.Specialties.Mappings;
using Application.Specialties.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Specialties.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SpecialtiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Especialidades", PermissionAction.View)]
    [EndpointSummary("Obtiene las especialidades")]
    [EndpointDescription("Lista todas las especialidades registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SpecialtyResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SpecialtyResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllSpecialtiesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [RequirePermission("Especialidades", PermissionAction.View)]
    [EndpointSummary("Obtiene una especialidad")]
    [EndpointDescription("Busca una especialidad por su identificador.")]
    [ProducesResponseType(typeof(SpecialtyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialtyResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetSpecialtyByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [RequirePermission("Especialidades", PermissionAction.Create)]
    [EndpointSummary("Crea una especialidad")]
    [EndpointDescription("Registra una nueva especialidad.")]
    [ProducesResponseType(typeof(SpecialtyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpecialtyResponseDto>> Create(CreateSpecialtyDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSpecialtyCommand(dto.Name, dto.Description), ct);
        var specialty = await sender.Send(new GetSpecialtyByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, specialty.ToDto());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Especialidades", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una especialidad")]
    [EndpointDescription("Actualiza los datos de una especialidad existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateSpecialtyDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateSpecialtyCommand(id, dto.Name, dto.Description), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Especialidades", PermissionAction.Delete)]
    [EndpointSummary("Elimina una especialidad")]
    [EndpointDescription("Elimina una especialidad por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSpecialtyCommand(id), ct);
        return NoContent();
    }
}
