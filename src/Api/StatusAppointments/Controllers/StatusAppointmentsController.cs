using Api.Common.Security;
using Api.StatusAppointments.Dtos;
using Api.StatusAppointments.Mappings;
using Application.StatusAppointments.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.StatusAppointments.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusAppointmentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Crea un nuevo estado de cita")]
    [EndpointDescription("Registra un nuevo estado para las citas veterinarias (ej. Pendiente, Confirmada, En Progreso, Completada, Cancelada).")]
    [ProducesResponseType(typeof(CreateStatusAppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateStatusAppointmentResponse>> Create(
        [FromBody] CreateStatusAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateStatusAppointmentResponse(id));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene todos los estados de cita")]
    [EndpointDescription("Retorna el listado completo de todos los estados de cita configurados en el sistema, ordenados alfabéticamente por nombre.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StatusAppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StatusAppointmentResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var statuses = await sender.Send(
            new GetAllStatusAppointmentsQuery(),
            cancellationToken);

        return Ok(statuses.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene un estado de cita por su ID")]
    [EndpointDescription("Retorna la información detallada de un estado de cita específico identificado por su GUID.")]
    [ProducesResponseType(typeof(StatusAppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StatusAppointmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var status = await sender.Send(
            new GetStatusAppointmentByIdQuery(id),
            cancellationToken);

        return status is null
            ? NotFound()
            : Ok(status.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Actualiza un estado de cita existente")]
    [EndpointDescription("Modifica el nombre y/o la descripción de un estado de cita previamente registrado. No permite nombres duplicados.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStatusAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Elimina un estado de cita por su ID")]
    [EndpointDescription("Remueve permanentemente un estado de cita del sistema. Esta acción no se puede deshacer.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteStatusAppointmentCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
