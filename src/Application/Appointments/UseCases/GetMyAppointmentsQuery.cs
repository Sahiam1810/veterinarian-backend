using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetMyAppointmentsQuery(
    Guid ClientId,
    AppointmentQueryScope Scope = AppointmentQueryScope.All)
    : IRequest<IReadOnlyCollection<Appointment>>;

public sealed class GetMyAppointmentsQueryHandler(
    IUnitOfWork uow,
    TimeProvider timeProvider)
    : IRequestHandler<GetMyAppointmentsQuery, IReadOnlyCollection<Appointment>>
{
    public async Task<IReadOnlyCollection<Appointment>> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var client = await uow.ClientsRepository.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        var clientPets = await uow.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        if (clientPets.Count == 0)
        {
            return Array.Empty<Appointment>();
        }

        var clientPetIds = clientPets.Select(cp => cp.Id).ToArray();
        var appointments = await uow.AppointmentsRepository.GetByClientPetIdsAsync(
            clientPetIds,
            cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return request.Scope switch
        {
            AppointmentQueryScope.Upcoming => appointments
                .Where(appointment =>
                    string.Equals(
                        appointment.Status?.Name,
                        AppointmentStatusNames.Agendada,
                        StringComparison.OrdinalIgnoreCase)
                    && appointment.ScheduledEnd >= now)
                .OrderBy(appointment => appointment.ScheduledStart)
                .ToArray(),
            AppointmentQueryScope.History => appointments
                .Where(appointment =>
                    !string.Equals(
                        appointment.Status?.Name,
                        AppointmentStatusNames.Agendada,
                        StringComparison.OrdinalIgnoreCase)
                    || appointment.ScheduledEnd < now)
                .OrderByDescending(appointment => appointment.ScheduledStart)
                .ToArray(),
            _ => appointments
                .OrderByDescending(appointment => appointment.ScheduledStart)
                .ToArray()
        };
    }
}
