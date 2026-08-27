using Application.Common.Abstractions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class GetAppointmentByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAppointmentByIdQuery, Appointment?>
{
    public Task<Appointment?> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
