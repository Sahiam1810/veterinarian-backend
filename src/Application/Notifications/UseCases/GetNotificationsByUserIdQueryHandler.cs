using Application.Common.Abstractions;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class GetNotificationsByUserIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetNotificationsByUserIdQuery, IReadOnlyCollection<Notification>>
{
    public Task<IReadOnlyCollection<Notification>> Handle(
        GetNotificationsByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.NotificationsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
