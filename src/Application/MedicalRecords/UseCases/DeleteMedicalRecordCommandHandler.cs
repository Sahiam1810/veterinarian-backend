using Application.Common.Abstractions;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class DeleteMedicalRecordCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMedicalRecordCommand, bool>
{
    public async Task<bool> Handle(
        DeleteMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        var record = await unitOfWork.MedicalRecordsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (record is null)
        {
            return false;
        }

        await unitOfWork.MedicalRecordsRepository.DeleteAsync(
            record,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
