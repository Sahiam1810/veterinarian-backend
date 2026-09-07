using Application.Appointments.Abstraction;
using Application.Appointments.Scheduling;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record GetAvailableScheduleSlotsQuery(
    Guid VeterinarianId,
    DateOnly Date,
    Guid? ServiceId = null) : IRequest<IReadOnlyCollection<AppointmentBookingSlot>>;

public sealed class GetAvailableScheduleSlotsQueryHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences,
    IAppointmentBookingSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<GetAvailableScheduleSlotsQuery, IReadOnlyCollection<AppointmentBookingSlot>>
{
    private const int StaffHorizonDays = 365;

    public async Task<IReadOnlyCollection<AppointmentBookingSlot>> Handle(
        GetAvailableScheduleSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.VeterinarianId,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");
        if (veterinarian.User?.IsActive != true)
        {
            throw new BadRequestException("El veterinario no esta disponible.");
        }

        int? serviceDurationMinutes = null;
        if (request.ServiceId.HasValue)
        {
            var service = await unitOfWork.ServicesRepository.GetByIdAsync(
                request.ServiceId.Value,
                cancellationToken)
                ?? throw new NotFoundException("Servicio no encontrado.");
            if (!service.IsActive)
            {
                throw new BadRequestException("El servicio no esta disponible.");
            }

            serviceDurationMinutes = service.DurationMinutes;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        if (request.Date < today || request.Date > today.AddDays(StaffHorizonDays))
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

        return AppointmentSlotPlanner.Build(
            availabilities,
            request.Date,
            timeZone,
            DateTime.MinValue,
            serviceDurationMinutes,
            occupied,
            veterinarianAbsences,
            roomOverlaps);
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
}
