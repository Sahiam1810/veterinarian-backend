using Domain.Availabilities.ValueObjects;
using Domain.Common;
using Domain.Veterinarians.Entities;

namespace Domain.Availabilities.Entities;

public sealed class Availability : BaseEntity<Guid>
{
    public const int DefaultSlotDurationMinutes = 30;
    public const int MinSlotDurationMinutes = 5;
    public const int MaxSlotDurationMinutes = 240;
    public const int DefaultMaxConcurrentAppointments = 1;
    public const int MinConcurrentAppointments = 1;
    public const int MaxConcurrentAppointmentsLimit = 20;

    private Availability()
    {
    }

    public Availability(
        Guid veterinarianId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isActive = true,
        int slotDurationMinutes = DefaultSlotDurationMinutes,
        string? shiftName = null,
        string? consultingRoom = null,
        int maxConcurrentAppointments = DefaultMaxConcurrentAppointments)
    {
        var timeRange = TimeRange.Create(startTime, endTime);

        Id = Guid.NewGuid();
        VeterinarianId = veterinarianId;
        DayOfWeek = dayOfWeek;
        StartTime = timeRange.StartTime;
        EndTime = timeRange.EndTime;
        IsActive = isActive;
        ApplyScheduleFields(slotDurationMinutes, shiftName, consultingRoom, maxConcurrentAppointments);
    }

    public Guid VeterinarianId { get; private set; }
    public Veterinarian? Veterinarian { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    // Duracion de cada hueco interno cuando no hay servicio.
    public int SlotDurationMinutes { get; private set; }

    // Etiqueta opcional del turno (Manana/Tarde).
    public string? ShiftName { get; private set; }

    // Sala fisica opcional; el JSON publico usa consultingRoom.
    public string? ConsultingRoom { get; private set; }

    // Cuantas citas AGENDADA pueden coincidir en este horario.
    public int MaxConcurrentAppointments { get; private set; }

    public void Update(
        Guid veterinarianId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isActive,
        int slotDurationMinutes = DefaultSlotDurationMinutes,
        string? shiftName = null,
        string? consultingRoom = null,
        int maxConcurrentAppointments = DefaultMaxConcurrentAppointments)
    {
        var timeRange = TimeRange.Create(startTime, endTime);

        VeterinarianId = veterinarianId;
        DayOfWeek = dayOfWeek;
        StartTime = timeRange.StartTime;
        EndTime = timeRange.EndTime;
        IsActive = isActive;
        ApplyScheduleFields(slotDurationMinutes, shiftName, consultingRoom, maxConcurrentAppointments);
        UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyScheduleFields(
        int slotDurationMinutes,
        string? shiftName,
        string? consultingRoom,
        int maxConcurrentAppointments)
    {
        SlotDurationMinutes = NormalizeSlotDuration(slotDurationMinutes);
        ShiftName = ValueObjects.ShiftName.CreateOptional(shiftName)?.Value;
        ConsultingRoom = ValueObjects.ConsultingRoom.CreateOptional(consultingRoom)?.Value;
        MaxConcurrentAppointments = NormalizeMaxConcurrent(maxConcurrentAppointments);
    }

    private static int NormalizeSlotDuration(int minutes)
    {
        if (minutes is < MinSlotDurationMinutes or > MaxSlotDurationMinutes)
        {
            throw new ArgumentException(
                $"La duracion del hueco debe estar entre {MinSlotDurationMinutes} y {MaxSlotDurationMinutes} minutos.",
                nameof(minutes));
        }

        return minutes;
    }

    private static int NormalizeMaxConcurrent(int value)
    {
        if (value is < MinConcurrentAppointments or > MaxConcurrentAppointmentsLimit)
        {
            throw new ArgumentException(
                $"El maximo de citas concurrentes debe estar entre {MinConcurrentAppointments} y {MaxConcurrentAppointmentsLimit}.",
                nameof(value));
        }

        return value;
    }
}
