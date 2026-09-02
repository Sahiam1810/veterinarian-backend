using System.Text.Json;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Domain.Appointments.ValueObjects;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using MediatR;

namespace Application.Appointments.UseCases;

// Solicita OTP para cancelar o reagendar una cita propia (autoservicio).
public sealed record RequestAppointmentActionCodeCommand(
    Guid AppointmentId,
    string PhoneNumber,
    AppointmentVerificationAction Action,
    string? ActionPayloadJson = null) : IRequest<Guid>;

public sealed class RequestAppointmentActionCodeCommandHandler(
    IUnitOfWork unitOfWork,
    IAppointmentActionVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IVerificationCodeDispatcher codeDispatcher,
    IAppointmentVerificationSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<RequestAppointmentActionCodeCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestAppointmentActionCodeCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        EnsurePhoneMatches(appointment, request.PhoneNumber);

        if (request.Action is not AppointmentVerificationAction.Cancel
            and not AppointmentVerificationAction.Reschedule)
        {
            throw new BadRequestException("La acción de verificación no es válida.");
        }

        if (request.Action == AppointmentVerificationAction.Reschedule
            && string.IsNullOrWhiteSpace(request.ActionPayloadJson))
        {
            throw new BadRequestException(
                "El reagendado exige el payload con la nueva franja horaria.");
        }

        if (request.Action == AppointmentVerificationAction.Reschedule)
        {
            // Valida forma del payload antes de gastar un SMS.
            _ = JsonSerializer.Deserialize<AppointmentReschedulePayload>(
                request.ActionPayloadJson!);
        }

        var now = timeProvider.GetUtcNow();
        var active = await sessions.GetActiveByAppointmentAndActionAsync(
            request.AppointmentId,
            request.Action,
            cancellationToken);

        if (active is not null)
        {
            var resendAllowedAt = active.UpdatedAt.GetValueOrDefault(active.CreatedAt)
                .Add(settings.OtpResendInterval);
            if (active.ExpiresAt > now.UtcDateTime && now.UtcDateTime < resendAllowedAt)
            {
                throw new ConflictException(
                    "El código ya fue enviado. Espera un momento antes de solicitar otro.");
            }

            active.Cancel(now.UtcDateTime);
            await sessions.UpdateAsync(active, cancellationToken);
        }

        var otp = otpProtector.Create();
        var normalizedPhone = RequesterPhoneNumber.Normalize(request.PhoneNumber);
        var channel = VerificationDeliveryChannel.Sms;
        var expiresAt = now.Add(settings.OtpLifetime);

        try
        {
            await codeDispatcher.SendAsync(
                channel,
                normalizedPhone,
                otp.Code,
                expiresAt,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ConflictException(
                "No fue posible enviar el código en este momento. Intenta de nuevo.");
        }

        var session = AppointmentActionVerificationSession.Start(
            request.AppointmentId,
            request.Action,
            channel,
            otpProtector.HashPhone(normalizedPhone),
            otp.Hash,
            expiresAt.UtcDateTime,
            now.UtcDateTime,
            request.ActionPayloadJson);

        await sessions.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    internal static void EnsurePhoneMatches(
        Domain.Appointments.Entities.Appointment appointment,
        string phoneNumber)
    {
        if (appointment.RequesterPhoneNumber is null
            || !appointment.RequesterPhoneNumber.Matches(phoneNumber))
        {
            throw new UnauthorizedException(
                "El teléfono no coincide con el registrado al crear la cita.");
        }
    }
}

// Payload de reagendado embebido en la sesión OTP.
public sealed record AppointmentReschedulePayload(
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes);
