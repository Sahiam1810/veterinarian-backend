using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetMyVaccinationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyVaccinationsQuery, IReadOnlyCollection<Vaccination>>
{
    public async Task<IReadOnlyCollection<Vaccination>> Handle(
        GetMyVaccinationsQuery request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Perfil de cliente no encontrado.");

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);

        if (clientPets.Count == 0)
        {
            return Array.Empty<Vaccination>();
        }

        var clientPetIds = clientPets.Select(clientPet => clientPet.Id).ToArray();
        return await unitOfWork.VaccinationsRepository.GetByClientPetIdsAsync(
            clientPetIds,
            cancellationToken);
    }
}
