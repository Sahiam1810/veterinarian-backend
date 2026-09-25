using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class RegisterAppointmentPaymentCommandHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<RegisterAppointmentPaymentCommand>
{
    public async Task Handle(RegisterAppointmentPaymentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            throw new NotFoundException("Cita médica no encontrada.");
        }

        if (appointment.IsPaid)
        {
            throw new ConflictException("La cita ya está pagada.");
        }

        appointment.RegisterPayment();

        await unitOfWork.AppointmentsRepository.UpdateAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
