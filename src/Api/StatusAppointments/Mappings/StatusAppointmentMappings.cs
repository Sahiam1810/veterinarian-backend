using Api.StatusAppointments.Dtos;
using Application.StatusAppointments.UseCases;
using Domain.StatusAppointments.Entities;

namespace Api.StatusAppointments.Mappings;

public static class StatusAppointmentMappings
{
    public static CreateStatusAppointmentCommand ToCommand(
        this CreateStatusAppointmentRequest request)
    {
        return new CreateStatusAppointmentCommand(
            request.Name,
            request.Description);
    }

    public static UpdateStatusAppointmentCommand ToCommand(
        this UpdateStatusAppointmentRequest request,
        Guid id)
    {
        return new UpdateStatusAppointmentCommand(
            id,
            request.Name,
            request.Description);
    }

    public static StatusAppointmentResponse ToResponse(
        this StatusAppointment entity)
    {
        return new StatusAppointmentResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<StatusAppointmentResponse> ToResponse(
        this IReadOnlyCollection<StatusAppointment> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
