using Api.Appointments.Dtos;
using Api.Common.Security;
using Application.Appointments.UseCases;
using Domain.Verification.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace Api.Appointments.Controllers;

// Autoservicio de citas: OTP sin JWT (chatbot). Tarea 5.2 cerró la variante JWT (CancelMine).
[ApiController]
[Route("api/appointments/mine")]
public sealed class MyAppointmentsController(ISender sender) : ControllerBase
{
    [HttpPost("{id:guid}/request-code")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AppointmentOtpRequest)]
    [EndpointSummary("Solicita código OTP para actuar sobre una cita")]
    [EndpointDescription("Compara el teléfono entrante con RequesterPhoneNumber y envía el código por SMS.")]
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
    [EndpointSummary("Confirma el código OTP y ejecuta la acción")]
    [EndpointDescription("Valida el OTP y cancela o reagenda la cita. Nunca borra la fila.")]
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
