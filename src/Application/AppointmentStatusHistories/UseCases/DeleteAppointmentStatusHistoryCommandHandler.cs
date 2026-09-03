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

        // El primero (más reciente) es el que determina Appointment.StatusId;
        // borrarlo desincroniza la cita del historial sin dejar rastro. Para
        // "deshacer" el estado vigente hay que usar el cambio de estado.
        var appointmentHistory = await unitOfWork.AppointmentStatusHistoriesRepository.GetByAppointmentIdAsync(
            history.AppointmentId,
            cancellationToken);

        if (appointmentHistory.First().Id == history.Id)
        {
            throw new ConflictException(
                "No se puede eliminar el historial vigente de la cita; usá el cambio de estado para avanzarla.");
        }

        await unitOfWork.AppointmentStatusHistoriesRepository.DeleteAsync(
            history,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
