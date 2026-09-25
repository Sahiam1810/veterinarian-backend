using System.Security.Claims;
using Api.Common.Security.Permissions;
using Api.ProcedureOrders.Dtos;
using Api.ProcedureOrders.Mappings;
using Application.ProcedureOrders.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.ProcedureOrders.Controllers;

[ApiController]
[Route("api/procedure-orders")]
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

    [HttpGet("hospitalization-stay/{stayId:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene las órdenes de procedimiento de una estancia de hospitalización")]
    [EndpointDescription("Incluye las órdenes creadas en la estancia y las de su cita de origen.")]
    [ProducesResponseType(typeof(IEnumerable<ProcedureOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProcedureOrderDto>>> GetByHospitalizationStayId(
        [FromRoute] Guid stayId,
        CancellationToken cancellationToken = default)
    {
        var orders = await sender.Send(new GetProcedureOrdersByHospitalizationStayIdQuery(stayId), cancellationToken);
        return Ok(orders.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Órdenes Médicas", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva orden de procedimiento (interna o remitida)")]
    [ProducesResponseType(typeof(ProcedureOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProcedureOrderDto>> Create(
        [FromBody] CreateProcedureOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        var order = await sender.Send(dto.ToCommand(actorUserId), cancellationToken);
        var response = order.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}/complete")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Edit)]
    [EndpointSummary("Marca una orden de procedimiento como Completada y notifica al veterinario")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        [FromBody] CompleteProcedureOrderDto? dto,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new CompleteProcedureOrderCommand(id, dto?.ResultFileUrl), cancellationToken);
        return NoContent();
    }

    [HttpGet("pending")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    public async Task<ActionResult<IEnumerable<PendingProcedureOrderDto>>> GetPending(CancellationToken cancellationToken)
    {
        var query = new GetPendingProcedureOrdersQuery();
        var result = await sender.Send(query, cancellationToken);
        return Ok(result.Select(x => x.ToPendingDto()));
    }

    // Con MapInboundClaims=false el subject queda como "sub".
    private bool TryGetActorUserId(out Guid actorUserId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out actorUserId);
    }
}
