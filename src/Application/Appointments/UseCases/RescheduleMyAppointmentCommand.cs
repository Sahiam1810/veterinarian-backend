using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Appointments.Abstraction;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Availabilities.Entities;
using Domain.Appointments.ValueObjects;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record RescheduleMyAppointmentCommand(
    Guid AppointmentId,
    Guid UserAccountId,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string RequesterPhoneNumber,
    string? Notes) : IRequest;

public sealed class RescheduleMyAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences,
    IAppointmentBookingSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<RescheduleMyAppointmentCommand>
{
    public async Task Handle(
        RescheduleMyAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = RequesterPhoneNumber.Normalize(
            request.RequesterPhoneNumber ?? string.Empty);
        if (normalizedPhone.Length is < 7 or > RequesterPhoneNumber.MaxLength)
        {
            throw new BadRequestException(
                $"El teléfono de contacto debe tener entre 7 y {RequesterPhoneNumber.MaxLength} dígitos.");
        }

        if (request.ScheduledEnd <= request.ScheduledStart)
        {
            throw new BadRequestException("La franja horaria de reagendado no es válida.");
        }
        if (request.ScheduledStart.Kind != DateTimeKind.Utc
            || request.ScheduledEnd.Kind != DateTimeKind.Utc)
        {
            throw new BadRequestException("La franja horaria debe estar expresada en UTC.");
        }
        ValidateBookingWindow(request.ScheduledStart);

        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var appointment = await unitOfWork.AppointmentsRepository.LockByIdAsync(
                request.AppointmentId,
                transactionCancellationToken)
                ?? throw new NotFoundException("Cita médica no encontrada.");

            var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
                client.Id,
                transactionCancellationToken);
            if (clientPets.All(clientPet => clientPet.Id != appointment.ClientPetId))
            {
                throw new ForbiddenException("La cita no pertenece al cliente autenticado.");
            }

            var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
                appointment.StatusId,
                transactionCancellationToken)
                ?? throw new ConflictException("El estado actual de la cita no es válido.");
            if (!string.Equals(
                    currentStatus.Name,
                    AppointmentStatusNames.Agendada,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "Solo se puede reagendar una cita en estado AGENDADA.");
            }

            var service = await unitOfWork.ServicesRepository.GetByIdAsync(
                appointment.ServiceId,
                transactionCancellationToken)
                ?? throw new NotFoundException("Servicio no encontrado.");

            var availability = await AppointmentSchedulingConcurrency.LockAvailabilityAsync(
                unitOfWork,
                request.AvailabilityId,
                transactionCancellationToken);
            EnsureAvailabilityMatches(
                availability,
                appointment.VeterinarianId,
                request.ScheduledStart,
                request.ScheduledEnd,
                service.DurationMinutes);
            await AppointmentSchedulingConcurrency.EnsureAvailableAsync(
                unitOfWork,
                absences,
                availability,
                appointment.ClientPetId,
                appointment.VeterinarianId,
                request.ScheduledStart,
                request.ScheduledEnd,
                appointment.Id,
                consultingRoom: null,
                transactionCancellationToken);

            appointment.Reschedule(
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.Notes,
                availability.ConsultingRoom);
            appointment.ApplyRequesterPhone(normalizedPhone);

            await unitOfWork.AppointmentsRepository.UpdateAsync(
                appointment,
                transactionCancellationToken);
            await unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }

    private void EnsureAvailabilityMatches(
        Availability availability,
        Guid veterinarianId,
        DateTime startUtc,
        DateTime endUtc,
        int durationMinutes)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, timeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, timeZone);
        var startsOnSlotBoundary =
            (localStart - localStart.Date.Add(availability.StartTime.ToTimeSpan())).Ticks
            % TimeSpan.FromMinutes(durationMinutes).Ticks == 0;
        var matches = availability.IsActive
            && availability.VeterinarianId == veterinarianId
            && localStart.Date == localEnd.Date
            && availability.DayOfWeek == localStart.DayOfWeek
            && TimeOnly.FromDateTime(localStart) >= availability.StartTime
            && TimeOnly.FromDateTime(localEnd) <= availability.EndTime
            && endUtc - startUtc == TimeSpan.FromMinutes(durationMinutes)
            && startsOnSlotBoundary;
        if (!matches)
        {
            throw new ConflictException(
                "El horario no corresponde a la disponibilidad seleccionada.");
        }
    }

    private void ValidateBookingWindow(DateTime startUtc)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        var requestedDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(startUtc, timeZone));
        if (startUtc < nowUtc.Add(settings.MinimumLeadTime)
            || requestedDate > today.AddDays(settings.MaximumAdvanceDays))
        {
            throw new BadRequestException(
                "La fecha está fuera del horizonte de agendamiento.");
        }
    }
}
