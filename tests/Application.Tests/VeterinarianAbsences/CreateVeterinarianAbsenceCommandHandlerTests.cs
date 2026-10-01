using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Application.VeterinarianAbsences.UseCases;
using Domain.VeterinarianAbsences.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.VeterinarianAbsences;

public sealed class CreateVeterinarianAbsenceCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_absence_and_returns_id()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var absences = Substitute.For<IVeterinarianAbsenceRepository>();
        var command = new CreateVeterinarianAbsenceCommand(
            Guid.NewGuid(),
            new DateTime(2026, 9, 10, 13, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 10, 17, 0, 0, DateTimeKind.Utc),
            "Capacitacion",
            false);

        var handler = new CreateVeterinarianAbsenceCommandHandler(unitOfWork, absences);
        var id = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await absences.Received(1).AddAsync(
            Arg.Is<VeterinarianAbsence>(item =>
                item.Id == id
                && item.VeterinarianId == command.VeterinarianId
                && item.Reason == command.Reason
                && item.IsFullDay == command.IsFullDay),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
