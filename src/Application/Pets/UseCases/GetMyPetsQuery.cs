using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Pets.Entities;
using Application.Pets.Models;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record GetMyPetsQuery(Guid UserAccountId) : IRequest<IReadOnlyCollection<OwnedPetProfile>>;

public sealed class GetMyPetsQueryHandler : IRequestHandler<GetMyPetsQuery, IReadOnlyCollection<OwnedPetProfile>>
{
    private readonly IUnitOfWork _uow;

    public GetMyPetsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<OwnedPetProfile>> Handle(GetMyPetsQuery request, CancellationToken cancellationToken)
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
            return Array.Empty<OwnedPetProfile>();
        }

        var petIds = clientPets.Select(cp => cp.PetId).ToArray();
        var pets = await _uow.PetsRepository.GetByIdsAsync(petIds, cancellationToken);
        return pets.Select(pet => new OwnedPetProfile(
            pet.Id,
            pet.Name.Value,
            pet.Age,
            pet.Gender.Value,
            pet.Weight.Value,
            pet.Observations.Value,
            pet.SpeciesId,
            pet.Species.Name.Value,
            pet.RaceId,
            pet.Race.Name.Value,
            pet.UpdatedAt ?? pet.CreatedAt)).ToArray();
    }
}
