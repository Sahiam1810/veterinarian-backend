using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record AppointmentBookingSlot(
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc);

public sealed record GetAppointmentBookingSlotsQuery(
    Guid UserAccountId,
    Guid VeterinarianId,
    Guid ServiceId,
    DateOnly Date) : IRequest<IReadOnlyCollection<AppointmentBookingSlot>>;

public sealed class GetAppointmentBookingSlotsQueryHandler(
    IUnitOfWork unitOfWork,
    IAppointmentBookingSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<GetAppointmentBookingSlotsQuery, IReadOnlyCollection<AppointmentBookingSlot>>
{
    public async Task<IReadOnlyCollection<AppointmentBookingSlot>> Handle(
        GetAppointmentBookingSlotsQuery request,
        CancellationToken cancellationToken)
    {
        await EnsureClientAsync(request.UserAccountId, cancellationToken);
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.ServiceId,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");
        if (!service.IsActive)
        {
            throw new BadRequestException("El servicio no está disponible.");
        }
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.VeterinarianId,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");
        if (veterinarian.User?.IsActive != true)
        {
            throw new BadRequestException("El veterinario no está disponible.");
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        if (request.Date < today || request.Date > today.AddDays(settings.MaximumAdvanceDays))
        {
            throw new BadRequestException("La fecha está fuera del horizonte de agendamiento.");
        }

        var dayStartUtc = ToUtc(request.Date.ToDateTime(TimeOnly.MinValue), timeZone);
        var dayEndUtc = ToUtc(request.Date.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
        var occupied = await unitOfWork.AppointmentsRepository.GetScheduledOverlapsAsync(
            request.VeterinarianId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);
        var availabilities = await unitOfWork.AvailabilitiesRepository
            .GetAllByVeterinarianIdAsync(request.VeterinarianId, cancellationToken);
        var earliestUtc = nowUtc.Add(settings.MinimumLeadTime);
        var slots = new List<AppointmentBookingSlot>();

        foreach (var availability in availabilities.Where(item =>
                     item.IsActive && item.DayOfWeek == request.Date.DayOfWeek))
        {
            var cursor = request.Date.ToDateTime(availability.StartTime);
            var localEnd = request.Date.ToDateTime(availability.EndTime);
            while (cursor.AddMinutes(service.DurationMinutes) <= localEnd)
            {
                var startUtc = ToUtc(cursor, timeZone);
                var endUtc = ToUtc(cursor.AddMinutes(service.DurationMinutes), timeZone);
                if (startUtc >= earliestUtc
                    && occupied.All(item =>
                        item.ScheduledStart >= endUtc || item.ScheduledEnd <= startUtc))
                {
                    slots.Add(new AppointmentBookingSlot(startUtc, endUtc));
                }
                cursor = cursor.AddMinutes(service.DurationMinutes);
            }
        }

        return slots.DistinctBy(slot => slot.ScheduledStartUtc)
            .OrderBy(slot => slot.ScheduledStartUtc)
            .ToArray();
    }

    private async Task EnsureClientAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            userAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");
        _ = await unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken)
            ?? throw new NotFoundException("El usuario no tiene un perfil de cliente asociado.");
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
}
