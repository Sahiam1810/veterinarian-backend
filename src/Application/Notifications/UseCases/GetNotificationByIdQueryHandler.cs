using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class GetNotificationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetNotificationByIdQuery, Notification>
{
    public async Task<Notification> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.NotificationsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Notificación no encontrada.");
    }
}
