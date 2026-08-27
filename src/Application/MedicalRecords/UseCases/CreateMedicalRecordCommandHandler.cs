using Application.Common.Abstractions;
using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class CreateMedicalRecordCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicalRecordCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        var record = new MedicalRecord(
            request.ClientPetId,
            request.AppointmentId,
            request.DiagnosticId,
            request.Symptoms,
            request.Treatment,
            request.WeightAtVisit,
            request.Temperature);

        await unitOfWork.MedicalRecordsRepository.AddAsync(
            record,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return record.Id;
    }
}
