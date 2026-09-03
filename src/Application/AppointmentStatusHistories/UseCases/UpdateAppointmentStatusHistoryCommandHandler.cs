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

        // Reescribir a qué cita/estado/mascota apunta un historial ya
        // registrado es falsificar el historial, no corregirlo -- y antes
        // dejaba a Appointment.StatusId sin sincronizar con el cambio. Solo
        // el comentario es corregible; para mover la cita a otro estado hay
        // que usar PATCH /api/appointments/{id}/status.
        if (history.AppointmentId != request.AppointmentId
            || history.StatusId != request.StatusId
            || history.ClientPetId != request.ClientPetId)
        {
            throw new BadRequestException(
                "No se puede modificar la cita, el estado o la mascota de un historial ya registrado; solo el comentario.");
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
    }
}
