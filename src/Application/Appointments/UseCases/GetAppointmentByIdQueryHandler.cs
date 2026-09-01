using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class GetAppointmentByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAppointmentByIdQuery, Appointment>
{
    public async Task<Appointment> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");
    }
}
