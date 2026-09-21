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
        return await unitOfWork.MedicalRecordsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Historia médica no encontrada.");
    }
}
