using Application.Common.Abstractions;
using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class GetAllMedicalRecordsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllMedicalRecordsQuery, IReadOnlyCollection<MedicalRecord>>
{
    public Task<IReadOnlyCollection<MedicalRecord>> Handle(
        GetAllMedicalRecordsQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.MedicalRecordsRepository.GetAllAsync(cancellationToken);
    }
}
