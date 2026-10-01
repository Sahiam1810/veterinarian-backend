using Application.Appointments.Abstraction;
using Application.Appointments.Scheduling;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record AppointmentBookingSlot(
    Guid AvailabilityId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    string? ConsultingRoom = null,
    string? ShiftName = null);

public sealed record GetAppointmentBookingSlotsQuery(
    Guid ClientId,
    Guid VeterinarianId,
    Guid ServiceId,
    DateOnly Date) : IRequest<IReadOnlyCollection<AppointmentBookingSlot>>;

public sealed class GetAppointmentBookingSlotsQueryHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences,
    IAppointmentBookingSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<GetAppointmentBookingSlotsQuery, IReadOnlyCollection<AppointmentBookingSlot>>
{
    public async Task<IReadOnlyCollection<AppointmentBookingSlot>> Handle(
        GetAppointmentBookingSlotsQuery request,
        CancellationToken cancellationToken)
    {
        await EnsureClientAsync(request.ClientId, cancellationToken);
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.ServiceId,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");
        if (!service.IsActive)
        {
            throw new BadRequestException("El servicio no esta disponible.");
        }
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.VeterinarianId,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");
        if (veterinarian.User?.IsActive != true)
        {
            throw new BadRequestException("El veterinario no esta disponible.");
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        if (request.Date < today || request.Date > today.AddDays(settings.MaximumAdvanceDays))
        {
            throw new BadRequestException("La fecha esta fuera del horizonte de agendamiento.");
        }

        var dayStartUtc = ToUtc(request.Date.ToDateTime(TimeOnly.MinValue), timeZone);
        var dayEndUtc = ToUtc(request.Date.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
        var occupied = await unitOfWork.AppointmentsRepository.GetScheduledOverlapsAsync(
            request.VeterinarianId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);
        var roomOverlaps = await unitOfWork.AppointmentsRepository.GetScheduledRoomOverlapsAsync(
            dayStartUtc,
            dayEndUtc,
            cancellationToken);
        var veterinarianAbsences = await absences.GetOverlappingAsync(
            request.VeterinarianId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);
        var availabilities = await unitOfWork.AvailabilitiesRepository
            .GetAllByVeterinarianIdAsync(request.VeterinarianId, cancellationToken);
        var earliestUtc = nowUtc.Add(settings.MinimumLeadTime);

        return AppointmentSlotPlanner.Build(
            availabilities,
            request.Date,
            timeZone,
            earliestUtc,
            service.DurationMinutes,
            occupied,
            veterinarianAbsences,
            roomOverlaps);
    }

    private async Task EnsureClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        _ = await unitOfWork.ClientsRepository.GetByIdAsync(clientId, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
}
