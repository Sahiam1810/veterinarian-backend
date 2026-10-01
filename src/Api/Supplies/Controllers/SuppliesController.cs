using Api.Common.Security.Permissions;
using Api.Supplies.Dtos;
using Api.Supplies.Mappings;
using Application.Supplies.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Supplies.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SuppliesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Insumos", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los insumos del catálogo")]
    [ProducesResponseType(typeof(IEnumerable<SupplyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplyDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var supplies = await sender.Send(new GetAllSuppliesQuery(onlyActive), cancellationToken);
        return Ok(supplies.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Insumos", PermissionAction.View)]
    [EndpointSummary("Obtiene un insumo por su ID")]
    [ProducesResponseType(typeof(SupplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplyDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var supply = await sender.Send(new GetSupplyByIdQuery(id), cancellationToken);
        return Ok(supply.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Insumos", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo insumo en el catálogo")]
    [ProducesResponseType(typeof(SupplyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplyDto>> Create(
        [FromBody] CreateSupplyDto dto,
        CancellationToken cancellationToken = default)
    {
        var supply = await sender.Send(dto.ToCommand(), cancellationToken);
        var response = supply.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Insumos", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un insumo existente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSupplyDto dto,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(dto.ToCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Insumos", PermissionAction.Delete)]
    [EndpointSummary("Desactiva un insumo del catálogo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeleteSupplyCommand(id), cancellationToken);
        return NoContent();
    }
}
