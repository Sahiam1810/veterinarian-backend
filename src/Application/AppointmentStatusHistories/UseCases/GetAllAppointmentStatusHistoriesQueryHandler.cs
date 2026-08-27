using Application.Common.Abstractions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class GetAllAppointmentStatusHistoriesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllAppointmentStatusHistoriesQuery, IReadOnlyCollection<AppointmentStatusHistory>>
{
    public Task<IReadOnlyCollection<AppointmentStatusHistory>> Handle(
        GetAllAppointmentStatusHistoriesQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AppointmentStatusHistoriesRepository.GetAllAsync(cancellationToken);
    }
}
