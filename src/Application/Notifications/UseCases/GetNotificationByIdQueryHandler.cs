using Application.Common.Abstractions;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class GetNotificationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetNotificationByIdQuery, Notification?>
{
    public Task<Notification?> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.NotificationsRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}
