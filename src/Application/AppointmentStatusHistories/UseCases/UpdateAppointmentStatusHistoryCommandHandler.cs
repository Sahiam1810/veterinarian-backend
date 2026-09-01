using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class UpdateAppointmentStatusHistoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentStatusHistoryCommand>
{
    public async Task Handle(
        UpdateAppointmentStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = await unitOfWork.AppointmentStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Historial de estado de cita no encontrado.");

        history.Update(
            request.AppointmentId,
            request.StatusId,
            request.ClientPetId,
            request.Comment);

        await unitOfWork.AppointmentStatusHistoriesRepository.UpdateAsync(
            history,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
