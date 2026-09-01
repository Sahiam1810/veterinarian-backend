using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Common.Models;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetMyAppointmentsAsVeterinarianQuery(
    Guid UserAccountId,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<Appointment>>;

public sealed class GetMyAppointmentsAsVeterinarianQueryHandler
    : IRequestHandler<GetMyAppointmentsAsVeterinarianQuery, PaginatedResult<Appointment>>
{
    private readonly IUnitOfWork _uow;

    public GetMyAppointmentsAsVeterinarianQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PaginatedResult<Appointment>> Handle(
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

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        return await _uow.AppointmentsRepository.GetByVeterinarianIdPagedAsync(
            veterinarian.Id,
            request.From,
            request.To,
            page,
            pageSize,
            cancellationToken);
    }
}
