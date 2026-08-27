using Api.AppointmentStatusHistories.Dtos;
using Application.AppointmentStatusHistories.UseCases;
using Domain.AppointmentStatusHistories.Entities;

namespace Api.AppointmentStatusHistories.Mappings;

public static class AppointmentStatusHistoryMappings
{
    public static CreateAppointmentStatusHistoryCommand ToCommand(
        this CreateAppointmentStatusHistoryRequest request)
    {
        return new CreateAppointmentStatusHistoryCommand(
            request.AppointmentId,
            request.StatusId,
            request.ClientPetId,
            request.Comment);
    }

    public static UpdateAppointmentStatusHistoryCommand ToCommand(
        this UpdateAppointmentStatusHistoryRequest request,
        Guid id)
    {
        return new UpdateAppointmentStatusHistoryCommand(
            id,
            request.AppointmentId,
            request.StatusId,
            request.ClientPetId,
            request.Comment);
    }

    public static AppointmentStatusHistoryResponse ToResponse(
        this AppointmentStatusHistory entity)
    {
        return new AppointmentStatusHistoryResponse(
            entity.Id,
            entity.AppointmentId,
            entity.StatusId,
            entity.Status?.Name,
            entity.ClientPetId,
            entity.Comment,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<AppointmentStatusHistoryResponse> ToResponse(
        this IReadOnlyCollection<AppointmentStatusHistory> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
