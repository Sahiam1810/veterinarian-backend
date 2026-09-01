using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class DeleteAppointmentStatusHistoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAppointmentStatusHistoryCommand>
{
    public async Task Handle(
        DeleteAppointmentStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = await unitOfWork.AppointmentStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Historial de estado de cita no encontrado.");

        await unitOfWork.AppointmentStatusHistoriesRepository.DeleteAsync(
            history,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
