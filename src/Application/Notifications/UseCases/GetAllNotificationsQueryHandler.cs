using Application.Common.Abstractions;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class GetAllNotificationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllNotificationsQuery, IReadOnlyCollection<Notification>>
{
    public Task<IReadOnlyCollection<Notification>> Handle(
        GetAllNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.NotificationsRepository.GetAllAsync(cancellationToken);
    }
}
