using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Pets.Entities;
using Application.Pets.Models;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record GetMyPetsQuery(Guid ClientId) : IRequest<IReadOnlyCollection<OwnedPetProfile>>;

public sealed class GetMyPetsQueryHandler : IRequestHandler<GetMyPetsQuery, IReadOnlyCollection<OwnedPetProfile>>
{
    private readonly IUnitOfWork _uow;

    public GetMyPetsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<OwnedPetProfile>> Handle(GetMyPetsQuery request, CancellationToken cancellationToken)
    {
        var client = await _uow.ClientsRepository.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

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
            // EF deja Observations en null cuando OBSERVATIONS es NULL en Oracle.
            pet.Observations?.Value,
            pet.SpeciesId,
            pet.Species.Name.Value,
            pet.RaceId,
            pet.Race.Name.Value,
            pet.UpdatedAt ?? pet.CreatedAt,
            pet.PhotoUrl.Value)).ToArray();
    }
}
