using Api.Appointments.Dtos;
using Api.Common.Security;
using Application.Appointments.UseCases;
using Domain.Verification.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;

namespace Api.Appointments.Controllers;

// Autoservicio de citas: JWT (mine) y OTP sin JWT (chatbot).
[ApiController]
[Route("api/appointments/mine")]
public sealed class MyAppointmentsController(ISender sender) : ControllerBase
{
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ClientOnly)]
    [EndpointSummary("Cancela una cita propia (cliente autenticado)")]
    [EndpointDescription("Soft-cancel: cambia el estado a CANCELADA y conserva el historial. No borra la fila.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelMine(
        Guid id,
        [FromBody] CancelMyAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        await sender.Send(
            new CancelMyAppointmentCommand(id, userAccountId, request?.Comment),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/request-code")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AppointmentOtpRequest)]
    [EndpointSummary("OTP de cita (chatbot/autoservicio): solicita código")]
    [EndpointDescription(
        "Flujo anónimo rate-limited para cancelar o reagendar una cita vía chatbot/autoservicio. " +
        "Compara el teléfono del body con Appointment.RequesterPhoneNumber y despacha OTP por el canal SMS de verificación de citas " +
        "(sesión propia APPOINTMENT_ACTION_VERIFICATION_SESSIONS). " +
        "No es ContactVerification: no verifica correo/Gmail y no crea, consume ni reutiliza ContactVerificationSession.")]
    [ProducesResponseType(typeof(RequestAppointmentActionCodeResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RequestAppointmentActionCodeResponse>> RequestCode(
        Guid id,
        [FromBody] RequestAppointmentActionCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AppointmentVerificationAction>(request.Action, true, out var action))
        {
            return BadRequest("La acción debe ser Cancel o Reschedule.");
        }

        string? payloadJson = null;
        if (action == AppointmentVerificationAction.Reschedule)
        {
            if (request.Reschedule is null)
            {
                return BadRequest("Reschedule requiere el objeto Reschedule.");
            }

            payloadJson = JsonSerializer.Serialize(new AppointmentReschedulePayload(
                request.Reschedule.AvailabilityId,
                request.Reschedule.ScheduledStart,
                request.Reschedule.ScheduledEnd,
                request.Reschedule.Notes));
        }

        var sessionId = await sender.Send(
            new RequestAppointmentActionCodeCommand(
                id,
                request.PhoneNumber,
                action,
                payloadJson),
            cancellationToken);

        return Accepted(new RequestAppointmentActionCodeResponse(sessionId));
    }

    [HttpPost("{id:guid}/confirm-code")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AppointmentOtpConfirm)]
    [EndpointSummary("OTP de cita (chatbot/autoservicio): confirma código y ejecuta acción")]
    [EndpointDescription(
        "Confirma el OTP de la sesión de cita (AppointmentActionVerificationSession) y cancela o reagenda. " +
        "Anónimo y rate-limited; el teléfono del body debe coincidir con RequesterPhoneNumber de la cita. " +
        "Independiente de ContactVerification Email/Gmail: no usa ni consume ContactVerificationSession. " +
        "Nunca borra la fila de la cita.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmCode(
        Guid id,
        [FromBody] ConfirmAppointmentActionCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AppointmentVerificationAction>(request.Action, true, out var action))
        {
            return BadRequest("La acción debe ser Cancel o Reschedule.");
        }

        await sender.Send(
            new ConfirmAppointmentActionCodeCommand(
                id,
                request.PhoneNumber,
                request.Code,
                action,
                request.Comment),
            cancellationToken);

        return NoContent();
    }
}
