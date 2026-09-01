using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetMyAppointmentsAsVeterinarianQuery(
    Guid UserAccountId,
    DateTime? From = null,
    DateTime? To = null) : IRequest<IReadOnlyCollection<Appointment>>;

public sealed class GetMyAppointmentsAsVeterinarianQueryHandler : IRequestHandler<GetMyAppointmentsAsVeterinarianQuery, IReadOnlyCollection<Appointment>>
{
    private readonly IUnitOfWork _uow;

    public GetMyAppointmentsAsVeterinarianQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<Appointment>> Handle(
        GetMyAppointmentsAsVeterinarianQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException("Cuenta de usuario no encontrada.");
        }

        var veterinarian = await _uow.VeterinariansRepository.GetByUserIdAsync(account.UserId, cancellationToken);
        if (veterinarian is null)
        {
            throw new NotFoundException("El usuario autenticado no tiene un perfil de veterinario asociado.");
        }

        return await _uow.AppointmentsRepository.GetByVeterinarianIdAsync(
            veterinarian.Id,
            request.From,
            request.To,
            cancellationToken);
    }
}
