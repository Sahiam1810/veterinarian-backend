namespace Api.Appointments.Dtos;

public sealed record RecordAppointmentVitalsRequest(
    decimal? Weight,
    decimal? Temperature,
    int? HeartRate,
    int? RespiratoryRate);
