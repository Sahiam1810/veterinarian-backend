using Application.Common.Abstractions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetAllVaccinationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllVaccinationsQuery, IReadOnlyCollection<Vaccination>>
{
    public async Task<IReadOnlyCollection<Vaccination>> Handle(
        GetAllVaccinationsQuery request,
        CancellationToken cancellationToken)
    {
        // Si el usuario autenticado tiene un perfil de Cliente asociado, solo ve
        // las vacunas de sus propias mascotas. El personal (sin perfil de
        // Cliente) sigue viendo el listado completo, sin filtrar.
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken);
        var client = account is null
            ? null
            : await unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);

        if (client is null)
        {
            return await unitOfWork.VaccinationsRepository.GetAllAsync(cancellationToken);
        }

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        if (clientPets.Count == 0)
        {
            return Array.Empty<Vaccination>();
        }

        var clientPetIds = clientPets.Select(cp => cp.Id).ToArray();
        return await unitOfWork.VaccinationsRepository.GetByClientPetIdsAsync(clientPetIds, cancellationToken);
    }
}
