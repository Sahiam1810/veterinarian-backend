using Application.Common.Abstractions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class GetAllAppointmentsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllAppointmentsQuery, IReadOnlyCollection<Appointment>>
{
    public Task<IReadOnlyCollection<Appointment>> Handle(
        GetAllAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AppointmentsRepository.GetAllAsync(cancellationToken);
    }
}
