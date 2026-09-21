using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Pets.Models;
using Domain.ContactVerification.Enums;
using MediatR;

namespace Application.Pets.UseCases;

// Consulta temporal de mascotas: consume proof Claim (un uso) sin bot-link.
public sealed record QueryPetsByClaimProof(Guid SessionId, string Proof)
    : IRequest<IReadOnlyCollection<OwnedPetProfile>>;

public sealed class QueryPetsByClaimProofHandler(
    IConsumeContactVerificationProof consumeProof,
    IUnitOfWork unitOfWork) : IRequestHandler<QueryPetsByClaimProof, IReadOnlyCollection<OwnedPetProfile>>
{
    public async Task<IReadOnlyCollection<OwnedPetProfile>> Handle(
        QueryPetsByClaimProof request,
        CancellationToken cancellationToken)
    {
        var consumed = await consumeProof.ConsumeAsync(
            new ConsumeContactVerificationProof(request.SessionId, request.Proof),
            cancellationToken);

        if (consumed.Purpose != ContactVerificationPurpose.Claim
            || consumed.SubjectUserId is null
            || consumed.SubjectUserId == Guid.Empty)
        {
            throw new ContactVerificationException(ContactVerificationErrors.PurposeInvalid);
        }

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
            consumed.SubjectUserId.Value,
            cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        if (clientPets.Count == 0)
        {
            return Array.Empty<OwnedPetProfile>();
        }

        var petIds = clientPets.Select(cp => cp.PetId).ToArray();
        var pets = await unitOfWork.PetsRepository.GetByIdsAsync(petIds, cancellationToken);
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
