using Application.Common.Abstractions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class DeleteAppointmentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAppointmentCommand, bool>
{
    public async Task<bool> Handle(
        DeleteAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (appointment is null)
        {
            return false;
        }

        await unitOfWork.AppointmentsRepository.DeleteAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
