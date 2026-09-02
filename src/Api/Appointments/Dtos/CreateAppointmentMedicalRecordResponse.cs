namespace Api.Appointments.Dtos;

public sealed record CreateAppointmentMedicalRecordResponse(
    Guid MedicalRecordId,
    Guid AppointmentId,
    IReadOnlyCollection<Guid> VaccinationIds);
