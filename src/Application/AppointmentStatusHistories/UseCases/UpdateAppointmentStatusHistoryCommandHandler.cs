using Application.Common.Abstractions;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class UpdateAppointmentStatusHistoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentStatusHistoryCommand, bool>
{
    public async Task<bool> Handle(
        UpdateAppointmentStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = await unitOfWork.AppointmentStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (history is null)
        {
            return false;
        }

        history.Update(
            request.AppointmentId,
            request.StatusId,
            request.ClientPetId,
            request.Comment);

        await unitOfWork.AppointmentStatusHistoriesRepository.UpdateAsync(
            history,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
