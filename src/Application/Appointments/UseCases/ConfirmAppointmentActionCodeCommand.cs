using System.Text.Json;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Domain.AppointmentStatusHistories.Entities;
using Domain.Appointments.ValueObjects;
using Domain.Verification.Enums;
using MediatR;

namespace Application.Appointments.UseCases;

// Confirma el OTP y ejecuta cancelar o reagendar la cita.
public sealed record ConfirmAppointmentActionCodeCommand(
    Guid AppointmentId,
    string PhoneNumber,
    string Code,
    AppointmentVerificationAction Action,
    string? Comment = null) : IRequest;

public sealed class ConfirmAppointmentActionCodeCommandHandler(
    IUnitOfWork unitOfWork,
    IAppointmentActionVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IAppointmentVerificationSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<ConfirmAppointmentActionCodeCommand>
{
    private const string Agendada = "AGENDADA";
    private const string Cancelada = "CANCELADA";

    public async Task Handle(
        ConfirmAppointmentActionCodeCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        RequestAppointmentActionCodeCommandHandler.EnsurePhoneMatches(
            appointment,
            request.PhoneNumber);

        var session = await sessions.GetActiveByAppointmentAndActionAsync(
            request.AppointmentId,
            request.Action,
            cancellationToken)
            ?? throw new NotFoundException("No hay una verificación activa para esta cita.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedPhone = RequesterPhoneNumber.Normalize(request.PhoneNumber);
        if (!string.Equals(
                session.DestinationHash,
                otpProtector.HashPhone(normalizedPhone),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException(
                "El teléfono no coincide con el de la verificación activa.");
        }

        if (session.ExpiresAt is null || now >= session.ExpiresAt)
        {
            session.Expire(now);
            await sessions.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ConflictException("El código venció. Solicita uno nuevo.");
        }

        if (session.OtpHash is null || !otpProtector.Verify(request.Code, session.OtpHash))
        {
            session.RegisterFailedAttempt(settings.OtpMaximumAttempts, now);
            await sessions.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (session.Status == VerificationSessionStatus.Blocked)
            {
                throw new ConflictException("Se agotaron los intentos. Solicita un código nuevo.");
            }

            throw new UnauthorizedException("El código no es válido.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (request.Action == AppointmentVerificationAction.Cancel)
            {
                await CancelAppointmentAsync(appointment, request.Comment, ct);
            }
            else if (request.Action == AppointmentVerificationAction.Reschedule)
            {
                await RescheduleAppointmentAsync(appointment, session.ActionPayload, ct);
            }
            else
            {
                throw new BadRequestException("La acción de verificación no es válida.");
            }

            session.Complete(now);
            await sessions.UpdateAsync(session, ct);
        }, cancellationToken);
    }

    private async Task CancelAppointmentAsync(
        Domain.Appointments.Entities.Appointment appointment,
        string? comment,
        CancellationToken cancellationToken)
    {
        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");

        if (!string.Equals(currentStatus.Name, Agendada, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Solo se puede cancelar una cita en estado AGENDADA.");
        }

        var statuses = await unitOfWork.StatusAppointmentsRepository.GetAllAsync(cancellationToken);
        var cancelStatus = statuses.FirstOrDefault(s =>
            string.Equals(s.Name, Cancelada, StringComparison.OrdinalIgnoreCase))
            ?? throw new ConflictException("No está configurado el estado CANCELADA.");

        if (string.IsNullOrWhiteSpace(comment))
        {
            comment = "Cancelada por el cliente vía verificación OTP.";
        }

        var history = new AppointmentStatusHistory(
            appointment.Id,
            cancelStatus.Id,
            appointment.ClientPetId,
            comment);

        await unitOfWork.AppointmentStatusHistoriesRepository.AddAsync(history, cancellationToken);

        appointment.Update(
            appointment.ClientPetId,
            appointment.VeterinarianId,
            appointment.ServiceId,
            cancelStatus.Id,
            appointment.AvailabilityId,
            appointment.ScheduledStart,
            appointment.ScheduledEnd,
            appointment.Notes);

        await unitOfWork.AppointmentsRepository.UpdateAsync(appointment, cancellationToken);
    }

    private async Task RescheduleAppointmentAsync(
        Domain.Appointments.Entities.Appointment appointment,
        string? actionPayloadJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actionPayloadJson))
        {
            throw new BadRequestException("Falta el payload de reagendado.");
        }

        AppointmentReschedulePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AppointmentReschedulePayload>(actionPayloadJson);
        }
        catch (JsonException)
        {
            throw new BadRequestException("El payload de reagendado no es válido.");
        }

        if (payload is null)
        {
            throw new BadRequestException("El payload de reagendado no es válido.");
        }

        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");

        if (!string.Equals(currentStatus.Name, Agendada, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Solo se puede reagendar una cita en estado AGENDADA.");
        }

        var availability = await unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            payload.AvailabilityId,
            cancellationToken)
            ?? throw new NotFoundException("Disponibilidad no encontrada.");

        if (payload.ScheduledEnd <= payload.ScheduledStart)
        {
            throw new BadRequestException("La franja horaria de reagendado no es válida.");
        }

        var hasOverlap = await unitOfWork.AppointmentsRepository.HasOverlappingAppointmentAsync(
            appointment.ClientPetId,
            appointment.VeterinarianId,
            payload.ScheduledStart,
            payload.ScheduledEnd,
            excludeAppointmentId: appointment.Id,
            cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            throw new ConflictException(
                "Ya existe otra cita agendada para la mascota o el veterinario en el horario seleccionado.");
        }

        appointment.Reschedule(
            availability.Id,
            payload.ScheduledStart,
            payload.ScheduledEnd,
            payload.Notes);

        await unitOfWork.AppointmentsRepository.UpdateAsync(appointment, cancellationToken);
    }
}
