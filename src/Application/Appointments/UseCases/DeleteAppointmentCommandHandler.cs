using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class DeleteAppointmentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAppointmentCommand>
{
    public async Task Handle(
        DeleteAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        await unitOfWork.AppointmentsRepository.DeleteAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
