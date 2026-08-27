using Application.Common.Abstractions;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class GetNotificationsByAppointmentIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetNotificationsByAppointmentIdQuery, IReadOnlyCollection<Notification>>
{
    public Task<IReadOnlyCollection<Notification>> Handle(
        GetNotificationsByAppointmentIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.NotificationsRepository.GetByAppointmentIdAsync(request.AppointmentId, cancellationToken);
    }
}
