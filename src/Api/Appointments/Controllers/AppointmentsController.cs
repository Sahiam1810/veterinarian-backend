using System.Security.Claims;
using Api.Appointments.Dtos;
using Api.Appointments.Mappings;
using Api.Common.Security;
using Api.Common.Security.Permissions;
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

    [HttpGet("me")]
    [RequirePermission("Citas", PermissionAction.View)]
    [EndpointSummary("Obtiene las citas del veterinario autenticado")]
    [EndpointDescription("Retorna las citas médicas asignadas al veterinario autenticado. Acepta filtros opcionales from/to (UTC) y paginación page/pageSize.")]
    [ProducesResponseType(typeof(PaginatedAppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedAppointmentResponse>> GetMe(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new GetMyAppointmentsAsVeterinarianQuery(userAccountId, from, to, page, pageSize),
            cancellationToken);

        return Ok(result.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Citas", PermissionAction.Create)]
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserAccountId(out var actorUserAccountId))
        {
            return Unauthorized();
        }

        var appointment = await sender.Send(
            new GetAppointmentByIdQuery(
                id,
                actorUserAccountId,
                ShouldEnforceVeterinarianOwnership()),
            cancellationToken);

        return Ok(appointment.ToResponse());
    }

    [HttpPatch("{appointmentId:guid}/status")]
    [RequirePermission("Citas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza el estado de una cita médica")]
    [EndpointDescription("Aplica una transición de estado permitida sobre la cita y registra el historial correspondiente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        Guid appointmentId,
        [FromBody] UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserAccountId(out var actorUserAccountId))
        {
            return Unauthorized();
        }

        await sender.Send(
            request.ToCommand(
                appointmentId,
                actorUserAccountId,
                ShouldEnforceVeterinarianOwnership()),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{appointmentId:guid}/medical-record")]
    [RequirePermission("Historiales Clínicos", PermissionAction.Create)]
    [EndpointSummary("Crea la historia clínica de una cita")]
    [EndpointDescription("Registra la historia clínica de la cita indicada y, opcionalmente, vacunas asociadas en la misma operación atómica.")]
    [ProducesResponseType(typeof(CreateAppointmentMedicalRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateAppointmentMedicalRecordResponse>> CreateMedicalRecord(
        Guid appointmentId,
        [FromBody] CreateAppointmentMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserAccountId(out var actorUserAccountId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            request.ToCommand(
                appointmentId,
                actorUserAccountId,
                ShouldEnforceVeterinarianOwnership()),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Citas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una cita médica existente")]
    [EndpointDescription("Modifica los datos de una cita médica previamente registrada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserAccountId(out var actorUserAccountId))
        {
            return Unauthorized();
        }

        await sender.Send(
            request.ToCommand(
                id,
                actorUserAccountId,
                ShouldEnforceVeterinarianOwnership()),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Elimina una cita médica por su ID (solo SuperAdmin)")]
    [EndpointDescription("Borrado físico restringido. El flujo de cliente debe usar soft-cancel por cambio de estado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteAppointmentCommand(id),
            cancellationToken);

        return NoContent();
    }

    // Con MapInboundClaims=false el subject queda como "sub" (y RoleClaimType="role").
    // ClaimTypes.NameIdentifier se mantiene por compatibilidad si algún middleware lo remapea.
    private bool TryGetActorUserAccountId(out Guid actorUserAccountId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out actorUserAccountId);
    }

    // Solo "Veterinario" aplica ownership. SuperAdmin (claim super_admin=true) y
    // Administrador/Recepcionista no filtran por veterinario asignado.
    private bool ShouldEnforceVeterinarianOwnership()
    {
        if (User.HasClaim(claim => claim.Type == "super_admin" && claim.Value == "true"))
        {
            return false;
        }

        var role = User.FindFirstValue("role");
        return string.Equals(role, "Veterinario", StringComparison.Ordinal);
    }
}
