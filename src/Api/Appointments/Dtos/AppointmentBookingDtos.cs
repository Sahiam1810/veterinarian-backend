namespace Api.Appointments.Dtos;

public sealed record AppointmentBookingPetResponse(Guid Id, string Name);

public sealed record AppointmentBookingServiceResponse(
    Guid Id,
    string Name,
    int DurationMinutes);

public sealed record AppointmentBookingVeterinarianResponse(
    Guid Id,
    string FullName,
    string SpecialtyName);

public sealed record AppointmentBookingOptionsResponse(
    IReadOnlyCollection<AppointmentBookingPetResponse> Pets,
    IReadOnlyCollection<AppointmentBookingServiceResponse> Services,
    IReadOnlyCollection<AppointmentBookingVeterinarianResponse> Veterinarians,
    bool RequiresRequesterPhoneNumber);

public sealed record AppointmentBookingSlotResponse(
    Guid AvailabilityId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc);

public sealed record CreateMyAppointmentRequest(
    Guid PetId,
    Guid VeterinarianId,
    Guid ServiceId,
    DateTime ScheduledStartUtc,
    string? Notes = null,
    string? RequesterPhoneNumber = null);
