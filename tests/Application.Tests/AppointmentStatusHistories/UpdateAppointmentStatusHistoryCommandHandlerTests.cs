using Application.AppointmentStatusHistories.Abstraction;
using Application.AppointmentStatusHistories.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.AppointmentStatusHistories;

public sealed class UpdateAppointmentStatusHistoryCommandHandlerTests
{
    private static readonly Guid HistoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StatusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository = Substitute.For<IAppointmentStatusHistoryRepository>();
    private readonly UpdateAppointmentStatusHistoryCommandHandler sut;

    public UpdateAppointmentStatusHistoryCommandHandlerTests()
    {
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(appointmentStatusHistoriesRepository);
        sut = new UpdateAppointmentStatusHistoryCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_updates_only_the_comment_when_appointment_status_and_pet_are_unchanged()
    {
        var history = CreateHistory();
        appointmentStatusHistoriesRepository.GetByIdAsync(HistoryId, Arg.Any<CancellationToken>()).Returns(history);
        var command = new UpdateAppointmentStatusHistoryCommand(HistoryId, AppointmentId, StatusId, ClientPetId, "comentario nuevo");

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal("comentario nuevo", history.Comment);
        await appointmentStatusHistoriesRepository.Received(1).UpdateAsync(history, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_changing_the_appointment_id_of_an_already_recorded_history_entry()
    {
        var history = CreateHistory();
        appointmentStatusHistoriesRepository.GetByIdAsync(HistoryId, Arg.Any<CancellationToken>()).Returns(history);
        var command = new UpdateAppointmentStatusHistoryCommand(HistoryId, Guid.NewGuid(), StatusId, ClientPetId, "x");

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));

        await appointmentStatusHistoriesRepository.DidNotReceive().UpdateAsync(Arg.Any<AppointmentStatusHistory>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_changing_the_status_id_of_an_already_recorded_history_entry()
    {
        var history = CreateHistory();
        appointmentStatusHistoriesRepository.GetByIdAsync(HistoryId, Arg.Any<CancellationToken>()).Returns(history);
        var command = new UpdateAppointmentStatusHistoryCommand(HistoryId, AppointmentId, Guid.NewGuid(), ClientPetId, "x");

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_rejects_changing_the_client_pet_id_of_an_already_recorded_history_entry()
    {
        var history = CreateHistory();
        appointmentStatusHistoriesRepository.GetByIdAsync(HistoryId, Arg.Any<CancellationToken>()).Returns(history);
        var command = new UpdateAppointmentStatusHistoryCommand(HistoryId, AppointmentId, StatusId, Guid.NewGuid(), "x");

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_history_entry_is_missing()
    {
        appointmentStatusHistoriesRepository.GetByIdAsync(HistoryId, Arg.Any<CancellationToken>())
            .Returns((AppointmentStatusHistory?)null);
        var command = new UpdateAppointmentStatusHistoryCommand(HistoryId, AppointmentId, StatusId, ClientPetId, "x");

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    private static AppointmentStatusHistory CreateHistory()
    {
        var history = new AppointmentStatusHistory(AppointmentId, StatusId, ClientPetId, "comentario original");
        typeof(AppointmentStatusHistory)
            .GetProperty(nameof(AppointmentStatusHistory.Id))!
            .SetValue(history, HistoryId);
        return history;
    }
}
