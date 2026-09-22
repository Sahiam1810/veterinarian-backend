using Api.Common.Security.Permissions;
using Api.MedicationOrders.Dtos;
using Api.MedicationOrders.Mappings;
using Application.MedicationOrders.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.MedicationOrders.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MedicationOrdersController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene una orden de medicamentos por su ID")]
    [ProducesResponseType(typeof(MedicationOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationOrderDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await sender.Send(new GetMedicationOrderByIdQuery(id), cancellationToken);
        return Ok(order.ToResponse());
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene las órdenes de medicamentos asociadas a una consulta médica")]
    [ProducesResponseType(typeof(IEnumerable<MedicationOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicationOrderDto>>> GetByAppointmentId(
        [FromRoute] Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var orders = await sender.Send(new GetMedicationOrdersByAppointmentIdQuery(appointmentId), cancellationToken);
        return Ok(orders.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Órdenes Médicas", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva orden de medicamentos (interna o remitida)")]
    [ProducesResponseType(typeof(MedicationOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicationOrderDto>> Create(
        [FromBody] CreateMedicationOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        var order = await sender.Send(dto.ToCommand(), cancellationToken);
        var response = order.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}/complete")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Edit)]
    [EndpointSummary("Marca una orden de medicamentos como Entregada")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new CompleteMedicationOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
