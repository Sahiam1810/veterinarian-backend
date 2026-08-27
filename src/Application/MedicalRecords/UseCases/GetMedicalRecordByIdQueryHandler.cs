using Application.Common.Abstractions;
using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class GetMedicalRecordByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicalRecordByIdQuery, MedicalRecord?>
{
    public Task<MedicalRecord?> Handle(
        GetMedicalRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.MedicalRecordsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
