using Api.Appointments.Dtos;
using Application.Appointments.UseCases;

namespace Api.Appointments.Mappings;

public static class RecordAppointmentVitalsRequestMappings
{
    public static RecordAppointmentVitalsCommand ToCommand(
        this RecordAppointmentVitalsRequest request,
        Guid appointmentId)
    {
        return new RecordAppointmentVitalsCommand(
            appointmentId,
            request.Weight,
            request.Temperature,
            request.HeartRate,
            request.RespiratoryRate);
    }
}
