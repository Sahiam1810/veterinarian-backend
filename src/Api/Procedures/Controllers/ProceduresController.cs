using Api.Common.Security.Permissions;
using Api.Procedures.Dtos;
using Api.Procedures.Mappings;
using Application.Procedures.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Procedures.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProceduresController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los procedimientos del catálogo")]
    [ProducesResponseType(typeof(IEnumerable<ProcedureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProcedureDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var procedures = await sender.Send(new GetAllProceduresQuery(onlyActive), cancellationToken);
        return Ok(procedures.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene un procedimiento por su ID")]
    [ProducesResponseType(typeof(ProcedureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcedureDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var procedure = await sender.Send(new GetProcedureByIdQuery(id), cancellationToken);
        return Ok(procedure.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Órdenes Médicas", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo procedimiento en el catálogo")]
    [ProducesResponseType(typeof(ProcedureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcedureDto>> Create(
        [FromBody] CreateProcedureDto dto,
        CancellationToken cancellationToken = default)
    {
        var procedure = await sender.Send(dto.ToCommand(), cancellationToken);
        var response = procedure.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un procedimiento existente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProcedureDto dto,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(dto.ToCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Delete)]
    [EndpointSummary("Desactiva un procedimiento del catálogo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeleteProcedureCommand(id), cancellationToken);
        return NoContent();
    }
}
