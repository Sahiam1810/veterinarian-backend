using Api.Appointments.Dtos;
using Application.Appointments.UseCases;
using Application.Common.Models;
using Domain.Appointments.Entities;

namespace Api.Appointments.Mappings;

public static class AppointmentMappings
{
    public static CreateAppointmentCommand ToCommand(
        this CreateAppointmentRequest request)
    {
        return new CreateAppointmentCommand(
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            request.StatusId,
            request.AvailabilityId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes);
    }

    public static UpdateAppointmentCommand ToCommand(
        this UpdateAppointmentRequest request,
        Guid id)
    {
        return new UpdateAppointmentCommand(
            id,
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            request.StatusId,
            request.AvailabilityId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes);
    }

    public static AppointmentResponse ToResponse(
        this Appointment entity)
    {
        return new AppointmentResponse(
            entity.Id,
            entity.ClientPetId,
            entity.VeterinarianId,
            entity.ServiceId,
            entity.Service?.Name,
            entity.StatusId,
            entity.Status?.Name,
            entity.AvailabilityId,
            entity.ScheduledStart,
            entity.ScheduledEnd,
            entity.Notes,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<AppointmentResponse> ToResponse(
        this IReadOnlyCollection<Appointment> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }

    public static PaginatedAppointmentResponse ToResponse(
        this PaginatedResult<Appointment> result)
    {
        return new PaginatedAppointmentResponse(
            result.Items.ToResponse(),
            result.Pagination);
    }
}
