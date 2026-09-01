using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class GetMedicalRecordByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicalRecordByIdQuery, MedicalRecord>
{
    public async Task<MedicalRecord> Handle(
        GetMedicalRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        var record = await unitOfWork.MedicalRecordsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Historia médica no encontrada.");

        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken);
        var client = account is null
            ? null
            : await unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);

        // Sin perfil de Cliente (personal): ve el registro sin restricción.
        if (client is null)
        {
            return record;
        }

        // Con perfil de Cliente: solo si el registro pertenece a una de sus
        // mascotas. Si no, se trata como inexistente (404), no como ajeno.
        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        var ownsRecord = clientPets.Any(cp => cp.Id == record.ClientPetId);

        return ownsRecord
            ? record
            : throw new NotFoundException("Historia médica no encontrada.");
    }
}
