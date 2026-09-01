using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class GetAllMedicalRecordsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllMedicalRecordsQuery, IReadOnlyCollection<MedicalRecord>>
{
    public async Task<IReadOnlyCollection<MedicalRecord>> Handle(
        GetAllMedicalRecordsQuery request,
        CancellationToken cancellationToken)
    {
        // Si el usuario autenticado tiene un perfil de Cliente asociado, solo ve
        // las historias médicas de sus propias mascotas. El personal (sin perfil
        // de Cliente) sigue viendo el listado completo, sin filtrar.
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);

        if (client is null)
        {
            return await unitOfWork.MedicalRecordsRepository.GetAllAsync(cancellationToken);
        }

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        if (clientPets.Count == 0)
        {
            return Array.Empty<MedicalRecord>();
        }

        var clientPetIds = clientPets.Select(cp => cp.Id).ToArray();
        return await unitOfWork.MedicalRecordsRepository.GetByClientPetIdsAsync(clientPetIds, cancellationToken);
    }
}
