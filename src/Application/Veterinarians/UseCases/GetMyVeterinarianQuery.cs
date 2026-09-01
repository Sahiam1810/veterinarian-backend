using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record GetMyVeterinarianQuery(Guid UserAccountId) : IRequest<Veterinarian>;

public sealed class GetMyVeterinarianQueryHandler : IRequestHandler<GetMyVeterinarianQuery, Veterinarian>
{
    private readonly IUnitOfWork _uow;

    public GetMyVeterinarianQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Veterinarian> Handle(GetMyVeterinarianQuery request, CancellationToken cancellationToken)
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

        return veterinarian;
    }
}
