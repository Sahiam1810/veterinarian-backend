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
[Route("api/bot/appointments")]
public sealed class BotAppointmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Obtiene las citas para el agente de Telegram")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentResponse>>> GetOwned(
        [FromQuery] AppointmentQueryScope scope = AppointmentQueryScope.All,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        var appointments = await sender.Send(
            new GetMyAppointmentsQuery(userAccountId, scope),
            cancellationToken);
        return Ok(appointments.ToResponse());
    }

    [HttpGet("by-identification/{identificationNumber}")]
    [Authorize(Policy = AuthorizationPolicies.TelegramBotChannel)]
    [EndpointSummary("Lista citas por cédula para el agente de Telegram")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentResponse>>> GetByIdentification(
        string identificationNumber,
        [FromQuery] AppointmentQueryScope scope = AppointmentQueryScope.All,
        CancellationToken cancellationToken = default)
    {
        var appointments = await sender.Send(
            new GetAppointmentsByIdentificationQuery(identificationNumber, scope),
            cancellationToken);
        return Ok(appointments.ToResponse());
    }

    [HttpGet("booking/options/by-identification/{identificationNumber}")]
    [Authorize(Policy = AuthorizationPolicies.TelegramBotChannel)]
    [EndpointSummary("Opciones de agendamiento por cédula")]
    [ProducesResponseType(typeof(AppointmentBookingOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentBookingOptionsResponse>> GetBookingOptionsByIdentification(
        string identificationNumber,
        CancellationToken cancellationToken)
    {
        var options = await sender.Send(
            new GetAppointmentBookingOptionsByIdentificationQuery(identificationNumber),
            cancellationToken);
        return Ok(options.ToResponse());
    }

    [HttpGet("{appointmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Obtiene una cita propia para el agente de Telegram")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentResponse>> GetOwnedById(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        var appointment = await sender.Send(
            new GetMyAppointmentByIdQuery(appointmentId, userAccountId),
            cancellationToken);
        return Ok(appointment.ToResponse());
    }

    [HttpGet("booking/options")]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Obtiene opciones de agendamiento para el agente de Telegram")]
    [ProducesResponseType(typeof(AppointmentBookingOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentBookingOptionsResponse>> GetBookingOptions(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        var options = await sender.Send(
            new GetAppointmentBookingOptionsQuery(userAccountId),
            cancellationToken);
        return Ok(options.ToResponse());
    }

    [HttpGet("booking/slots")]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Obtiene horarios de agendamiento para el agente de Telegram")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentBookingSlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentBookingSlotResponse>>> GetBookingSlots(
        [FromQuery] Guid veterinarianId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        var slots = await sender.Send(
            new GetAppointmentBookingSlotsQuery(userAccountId, veterinarianId, serviceId, date),
            cancellationToken);
        return Ok(slots.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Agenda una cita desde el agente de Telegram")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponse>> Create(
        [FromBody] CreateMyAppointmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        var appointment = await sender.Send(
            request.ToCommand(userAccountId, idempotencyKey),
            cancellationToken);
        return Created($"/api/bot/appointments/{appointment.Id}", appointment.ToResponse());
    }

    [HttpPost("by-identification")]
    [Authorize(Policy = AuthorizationPolicies.TelegramBotChannel)]
    [EndpointSummary("Agenda una cita asociada a una cédula")]
    [EndpointDescription(
        "Requiere que el cliente ya exista (usar POST /api/bot/clients/find-or-create antes). " +
        "No crea persona ni inicia OTP.")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponse>> CreateByIdentification(
        [FromBody] CreateAppointmentByIdentificationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var appointment = await sender.Send(
            new CreateAppointmentByIdentificationCommand(
                request.IdentificationNumber,
                request.PetId,
                request.VeterinarianId,
                request.ServiceId,
                request.ScheduledStartUtc,
                request.Notes,
                request.RequesterPhoneNumber,
                idempotencyKey),
            cancellationToken);
        return Created($"/api/bot/appointments/{appointment.Id}", appointment.ToResponse());
    }

    [HttpPatch("{appointmentId:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Cancela una cita propia desde el agente de Telegram")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid appointmentId,
        [FromBody] CancelMyAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        await sender.Send(
            new CancelMyAppointmentCommand(appointmentId, userAccountId, request?.Comment),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{appointmentId:guid}/cancel-by-identification")]
    [Authorize(Policy = AuthorizationPolicies.TelegramBotChannel)]
    [EndpointSummary("Cancela una cita por cédula desde el agente de Telegram")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelByIdentification(
        Guid appointmentId,
        [FromBody] CancelAppointmentByIdentificationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new CancelAppointmentByIdentificationCommand(
                appointmentId,
                request.IdentificationNumber,
                request.Comment),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{appointmentId:guid}/reschedule")]
    [Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
    [EndpointSummary("Reprograma una cita propia desde el agente de Telegram")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reschedule(
        Guid appointmentId,
        [FromBody] RescheduleMyAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        await sender.Send(
            new RescheduleMyAppointmentCommand(
                appointmentId,
                userAccountId,
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.RequesterPhoneNumber,
                request.Notes),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{appointmentId:guid}/reschedule-by-identification")]
    [Authorize(Policy = AuthorizationPolicies.TelegramBotChannel)]
    [EndpointSummary("Reprograma una cita por cédula desde el agente de Telegram")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleByIdentification(
        Guid appointmentId,
        [FromBody] RescheduleAppointmentByIdentificationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RescheduleAppointmentByIdentificationCommand(
                appointmentId,
                request.IdentificationNumber,
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.RequesterPhoneNumber,
                request.Notes),
            cancellationToken);
        return NoContent();
    }

    private bool TryGetUserAccountId(out Guid userAccountId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out userAccountId) && userAccountId != Guid.Empty;
    }
}
