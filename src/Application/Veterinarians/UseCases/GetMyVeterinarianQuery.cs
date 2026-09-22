using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record GetMyVeterinarianQuery(Guid UserId) : IRequest<Veterinarian>;

public sealed class GetMyVeterinarianQueryHandler : IRequestHandler<GetMyVeterinarianQuery, Veterinarian>
{
    private readonly IUnitOfWork _uow;

    public GetMyVeterinarianQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Veterinarian> Handle(GetMyVeterinarianQuery request, CancellationToken cancellationToken)
    {
        // U4: el sub ya es el id del usuario; ya no hace falta pasar por UserAccounts.
        var veterinarian = await _uow.VeterinariansRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (veterinarian is null)
        {
            throw new NotFoundException("El usuario autenticado no tiene un perfil de veterinario asociado.");
        }

        return veterinarian;
    }
}
