using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetAppointmentsByIdentificationQuery(
    string IdentificationNumber,
    AppointmentQueryScope Scope = AppointmentQueryScope.All)
    : IRequest<IReadOnlyCollection<Appointment>>;

public sealed class GetAppointmentsByIdentificationQueryHandler(
    IUnitOfWork uow,
    TimeProvider timeProvider)
    : IRequestHandler<GetAppointmentsByIdentificationQuery, IReadOnlyCollection<Appointment>>
{
    public async Task<IReadOnlyCollection<Appointment>> Handle(
        GetAppointmentsByIdentificationQuery request,
        CancellationToken cancellationToken)
    {
        var client = await ResolveClientOrNotFoundAsync(
            uow,
            request.IdentificationNumber,
            cancellationToken);

        var clientPets = await uow.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        if (clientPets.Count == 0)
        {
            return Array.Empty<Appointment>();
        }

        var appointments = await uow.AppointmentsRepository.GetByClientPetIdsAsync(
            clientPets.Select(cp => cp.Id).ToArray(),
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

    internal static async Task<ClientEntity> ResolveClientOrNotFoundAsync(
        IUnitOfWork unitOfWork,
        string identificationNumber,
        CancellationToken cancellationToken)
    {
        var identification = ClientIdentificationNumber.Create(identificationNumber).Value;
        var client = await unitOfWork.ClientsRepository.GetByIdentificationNumberAsync(
            identification,
            cancellationToken);
        return client ?? throw new NotFoundException(
            "No hay un registro asociado a esa cédula.");
    }
}
