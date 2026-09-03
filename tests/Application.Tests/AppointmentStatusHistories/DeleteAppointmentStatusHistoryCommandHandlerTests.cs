using Application.AppointmentStatusHistories.Abstraction;
using Application.AppointmentStatusHistories.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.AppointmentStatusHistories;

public sealed class DeleteAppointmentStatusHistoryCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StatusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository = Substitute.For<IAppointmentStatusHistoryRepository>();
    private readonly DeleteAppointmentStatusHistoryCommandHandler sut;

    public DeleteAppointmentStatusHistoryCommandHandlerTests()
    {
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(appointmentStatusHistoriesRepository);
        sut = new DeleteAppointmentStatusHistoryCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_deletes_a_superseded_history_entry()
    {
        var current = CreateHistory(DateTime.UtcNow);
        var superseded = CreateHistory(DateTime.UtcNow.AddMinutes(-10));
        appointmentStatusHistoriesRepository.GetByIdAsync(superseded.Id, Arg.Any<CancellationToken>()).Returns(superseded);
        appointmentStatusHistoriesRepository.GetByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(new[] { current, superseded });

        var command = new DeleteAppointmentStatusHistoryCommand(superseded.Id);

        await sut.Handle(command, CancellationToken.None);

        await appointmentStatusHistoriesRepository.Received(1).DeleteAsync(superseded, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_deleting_the_current_history_entry_of_the_appointment()
    {
        var current = CreateHistory(DateTime.UtcNow);
        var superseded = CreateHistory(DateTime.UtcNow.AddMinutes(-10));
        appointmentStatusHistoriesRepository.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
        appointmentStatusHistoriesRepository.GetByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(new[] { current, superseded });

        var command = new DeleteAppointmentStatusHistoryCommand(current.Id);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await appointmentStatusHistoriesRepository.DidNotReceive().DeleteAsync(Arg.Any<AppointmentStatusHistory>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_history_entry_is_missing()
    {
        var id = Guid.NewGuid();
        appointmentStatusHistoriesRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((AppointmentStatusHistory?)null);
        var command = new DeleteAppointmentStatusHistoryCommand(id);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    private static AppointmentStatusHistory CreateHistory(DateTime createdAt)
    {
        var history = new AppointmentStatusHistory(AppointmentId, StatusId, ClientPetId, "comentario");
        typeof(AppointmentStatusHistory)
            .GetProperty(nameof(AppointmentStatusHistory.CreatedAt))!
            .SetValue(history, createdAt);
        return history;
    }
}
