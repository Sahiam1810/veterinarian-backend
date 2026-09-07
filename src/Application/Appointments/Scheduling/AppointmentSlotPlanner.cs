using Application.Appointments.UseCases;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.VeterinarianAbsences.Entities;

namespace Application.Appointments.Scheduling;

internal static class AppointmentSlotPlanner
{
    public static IReadOnlyCollection<AppointmentBookingSlot> Build(
        IReadOnlyCollection<Availability> availabilities,
        DateOnly date,
        TimeZoneInfo timeZone,
        DateTime earliestUtc,
        int? serviceDurationMinutes,
        IReadOnlyCollection<Appointment> occupied,
        IReadOnlyCollection<VeterinarianAbsence> absences,
        IReadOnlyCollection<Appointment> roomOverlaps)
    {
        var slots = new List<AppointmentBookingSlot>();

        foreach (var availability in availabilities.Where(item =>
                     item.IsActive && item.DayOfWeek == date.DayOfWeek))
        {
            var durationMinutes = serviceDurationMinutes ?? availability.SlotDurationMinutes;
            if (durationMinutes <= 0)
            {
                continue;
            }

            var cursor = date.ToDateTime(availability.StartTime);
            var localEnd = date.ToDateTime(availability.EndTime);
            while (cursor.AddMinutes(durationMinutes) <= localEnd)
            {
                var startUtc = ToUtc(cursor, timeZone);
                var endUtc = ToUtc(cursor.AddMinutes(durationMinutes), timeZone);
                if (startUtc >= earliestUtc
                    && IsFree(
                        availability,
                        startUtc,
                        endUtc,
                        occupied,
                        absences,
                        roomOverlaps))
                {
                    slots.Add(new AppointmentBookingSlot(
                        availability.Id,
                        startUtc,
                        endUtc,
                        availability.ConsultingRoom,
                        availability.ShiftName));
                }

                cursor = cursor.AddMinutes(durationMinutes);
            }
        }

        return slots
            .DistinctBy(slot => (slot.AvailabilityId, slot.ScheduledStartUtc))
            .OrderBy(slot => slot.ScheduledStartUtc)
            .ThenBy(slot => slot.ConsultingRoom)
            .ToArray();
    }

    private static bool IsFree(
        Availability availability,
        DateTime startUtc,
        DateTime endUtc,
        IReadOnlyCollection<Appointment> occupied,
        IReadOnlyCollection<VeterinarianAbsence> absences,
        IReadOnlyCollection<Appointment> roomOverlaps)
    {
        if (absences.Any(item => item.Overlaps(startUtc, endUtc)))
        {
            return false;
        }

        var occupancy = occupied.Count(item =>
            item.ScheduledStart < endUtc && item.ScheduledEnd > startUtc);
        if (occupancy >= availability.MaxConcurrentAppointments)
        {
            return false;
        }

        if (HasRoomCollision(availability.ConsultingRoom, startUtc, endUtc, roomOverlaps))
        {
            return false;
        }

        return true;
    }

    private static bool HasRoomCollision(
        string? consultingRoom,
        DateTime startUtc,
        DateTime endUtc,
        IReadOnlyCollection<Appointment> roomOverlaps)
    {
        if (string.IsNullOrWhiteSpace(consultingRoom))
        {
            return false;
        }

        return roomOverlaps.Any(item =>
            !string.IsNullOrWhiteSpace(item.ConsultingRoom)
            && string.Equals(item.ConsultingRoom, consultingRoom, StringComparison.OrdinalIgnoreCase)
            && item.ScheduledStart < endUtc
            && item.ScheduledEnd > startUtc);
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
}
