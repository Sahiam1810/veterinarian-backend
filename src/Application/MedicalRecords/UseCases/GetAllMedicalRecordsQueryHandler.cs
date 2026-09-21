using Application.Common.Abstractions;
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
        return await unitOfWork.MedicalRecordsRepository.GetAllAsync(cancellationToken);
    }
}
