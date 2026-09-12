using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.Abstraction;
using Application.Notifications.UseCases;
using Domain.Notifications.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

// S43: marcar como leída es una acción del dueño de la notificación, no la
// edición general que exige "Notificaciones.Edit" (que ningún rol tiene).
public sealed class MarkNotificationAsReadCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly INotificationRepository notifications = Substitute.For<INotificationRepository>();
    private readonly MarkNotificationAsReadCommandHandler sut;

    public MarkNotificationAsReadCommandHandlerTests()
    {
        unitOfWork.NotificationsRepository.Returns(notifications);
        sut = new MarkNotificationAsReadCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_marks_own_notification_as_read()
    {
        var personId = Guid.NewGuid();
        var notification = new Notification(
            personId,
            Guid.NewGuid(),
            "Recordatorio: tienes una cita.",
            DateTime.UtcNow,
            "Pendiente",
            "Recordatorio");

        notifications.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        await sut.Handle(
            new MarkNotificationAsReadCommand(notification.Id, personId),
            CancellationToken.None);

        Assert.Equal("Leída", notification.Status.Value);
        await notifications.Received(1).UpdateAsync(notification, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_notification_is_missing()
    {
        notifications.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(
                new MarkNotificationAsReadCommand(Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None));

        await notifications.DidNotReceive().UpdateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_ForbiddenException_when_notification_belongs_to_another_user()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var notification = new Notification(
            ownerId,
            Guid.NewGuid(),
            "Recordatorio: tienes una cita.",
            DateTime.UtcNow,
            "Pendiente",
            "Recordatorio");

        notifications.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(
                new MarkNotificationAsReadCommand(notification.Id, actorId),
                CancellationToken.None));

        Assert.Equal("Pendiente", notification.Status.Value);
        await notifications.DidNotReceive().UpdateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
