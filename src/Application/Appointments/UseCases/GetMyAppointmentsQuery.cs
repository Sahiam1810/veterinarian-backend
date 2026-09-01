using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetMyAppointmentsQuery(Guid UserAccountId) : IRequest<IReadOnlyCollection<Appointment>>;

public sealed class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery, IReadOnlyCollection<Appointment>>
{
    private readonly IUnitOfWork _uow;

    public GetMyAppointmentsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<Appointment>> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException("Cuenta de usuario no encontrada.");
        }

        var client = await _uow.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("El usuario autenticado no tiene un perfil de cliente asociado.");
        }

        var clientPets = await _uow.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        if (clientPets.Count == 0)
        {
            return Array.Empty<Appointment>();
        }

        var clientPetIds = clientPets.Select(cp => cp.Id).ToArray();
        return await _uow.AppointmentsRepository.GetByClientPetIdsAsync(clientPetIds, cancellationToken);
    }
}
