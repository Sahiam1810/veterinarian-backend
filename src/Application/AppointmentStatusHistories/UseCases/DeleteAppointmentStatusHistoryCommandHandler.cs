using Application.Common.Abstractions;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class DeleteAppointmentStatusHistoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAppointmentStatusHistoryCommand, bool>
{
    public async Task<bool> Handle(
        DeleteAppointmentStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = await unitOfWork.AppointmentStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (history is null)
        {
            return false;
        }

        await unitOfWork.AppointmentStatusHistoriesRepository.DeleteAsync(
            history,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
