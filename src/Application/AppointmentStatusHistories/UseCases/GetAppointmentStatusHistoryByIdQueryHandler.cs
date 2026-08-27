using Application.Common.Abstractions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class GetAppointmentStatusHistoryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAppointmentStatusHistoryByIdQuery, AppointmentStatusHistory?>
{
    public Task<AppointmentStatusHistory?> Handle(
        GetAppointmentStatusHistoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AppointmentStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
