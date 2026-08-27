using System.Security.Claims;
using Api.Appointments.Dtos;
using Api.Appointments.Mappings;
using Api.Common.Security;
using Application.Appointments.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Appointments.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppointmentsController(ISender sender) : ControllerBase
{
    // GET /api/appointments/mine
    [HttpGet("mine")]
    [Authorize(Policy = AuthorizationPolicies.ClientOnly)]
    [EndpointSummary("Obtiene las citas del cliente autenticado")]
    [EndpointDescription("Retorna las citas médicas de las mascotas del cliente correspondiente al usuario autenticado actual (portal de dueño).")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var appointments = await sender.Send(new GetMyAppointmentsQuery(userAccountId), cancellationToken);
        return Ok(appointments.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOrReceptionist)]
    [EndpointSummary("Crea una nueva cita médica")]
    [EndpointDescription("Registra una nueva cita médica veterinaria en el sistema.")]
    [ProducesResponseType(typeof(CreateAppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAppointmentResponse>> Create(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateAppointmentResponse(id));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene todas las citas médicas")]
    [EndpointDescription("Retorna el listado completo de todas las citas médicas registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var appointments = await sender.Send(
            new GetAllAppointmentsQuery(),
            cancellationToken);

        return Ok(appointments.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene una cita médica por su ID")]
    [EndpointDescription("Retorna la información detallada de una cita médica específica.")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var appointment = await sender.Send(
            new GetAppointmentByIdQuery(id),
            cancellationToken);

        return appointment is null
            ? NotFound()
            : Ok(appointment.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrReceptionist)]
    [EndpointSummary("Actualiza una cita médica existente")]
    [EndpointDescription("Modifica los datos de una cita médica previamente registrada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAppointmentRequest request,
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
    [Authorize(Policy = AuthorizationPolicies.AdminOrReceptionist)]
    [EndpointSummary("Elimina una cita médica por su ID")]
    [EndpointDescription("Remueve permanentemente una cita médica del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteAppointmentCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
