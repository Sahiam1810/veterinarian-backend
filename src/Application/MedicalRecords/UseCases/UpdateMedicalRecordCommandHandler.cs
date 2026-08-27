using Application.Common.Abstractions;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class UpdateMedicalRecordCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMedicalRecordCommand, bool>
{
    public async Task<bool> Handle(
        UpdateMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        var record = await unitOfWork.MedicalRecordsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (record is null)
        {
            return false;
        }

        record.Update(
            request.ClientPetId,
            request.AppointmentId,
            request.DiagnosticId,
            request.Symptoms,
            request.Treatment,
            request.WeightAtVisit,
            request.Temperature);

        await unitOfWork.MedicalRecordsRepository.UpdateAsync(
            record,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
