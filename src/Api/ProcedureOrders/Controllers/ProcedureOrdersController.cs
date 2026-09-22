using Api.Common.Security.Permissions;
using Api.ProcedureOrders.Dtos;
using Api.ProcedureOrders.Mappings;
using Application.ProcedureOrders.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.ProcedureOrders.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProcedureOrdersController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene una orden de procedimiento por su ID")]
    [ProducesResponseType(typeof(ProcedureOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcedureOrderDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await sender.Send(new GetProcedureOrderByIdQuery(id), cancellationToken);
        return Ok(order.ToResponse());
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene las órdenes de procedimiento asociadas a una consulta médica")]
    [ProducesResponseType(typeof(IEnumerable<ProcedureOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProcedureOrderDto>>> GetByAppointmentId(
        [FromRoute] Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var orders = await sender.Send(new GetProcedureOrdersByAppointmentIdQuery(appointmentId), cancellationToken);
        return Ok(orders.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Órdenes Médicas", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva orden de procedimiento (interna o remitida)")]
    [ProducesResponseType(typeof(ProcedureOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcedureOrderDto>> Create(
        [FromBody] CreateProcedureOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        var order = await sender.Send(dto.ToCommand(), cancellationToken);
        var response = order.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}/complete")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Edit)]
    [EndpointSummary("Marca una orden de procedimiento como Completada y notifica al veterinario")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        [FromBody] CompleteProcedureOrderDto? dto,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new CompleteProcedureOrderCommand(id, dto?.ResultFileUrl), cancellationToken);
        return NoContent();
    }
}
