using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Reports.Models;
using Application.Reports.UseCases;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace Application.Tests.Reports;

public sealed class GetAppointmentsByVeterinarianReportQueryTests
{
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetAppointmentsByVeterinarianReportQueryTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
    }

    [Theory]
    [InlineData("2026-02-02", "2026-02-01")]
    [InlineData("2025-01-01", "2026-01-03")]
    public void Validator_rejects_an_invalid_date_range(string from, string to)
    {
        var result = new GetAppointmentsByVeterinarianReportQueryValidator().TestValidate(
            new GetAppointmentsByVeterinarianReportQuery(DateOnly.Parse(from), DateOnly.Parse(to)));

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public async Task Handler_aggregates_statuses_for_multiple_veterinarians_and_sorts_by_total()
    {
        var veterinarianOne = Guid.NewGuid();
        var veterinarianTwo = Guid.NewGuid();
        appointmentsRepository.GetForVeterinarianReportAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AppointmentVeterinarianReportEntry(veterinarianOne, "Dra. Ana", "ATENDIDA"),
                new AppointmentVeterinarianReportEntry(veterinarianOne, "Dra. Ana", "CANCELADA"),
                new AppointmentVeterinarianReportEntry(veterinarianOne, "Dra. Ana", "CONFIRMADA"),
                new AppointmentVeterinarianReportEntry(veterinarianTwo, "Dr. Beto", "AGENDADA"),
                new AppointmentVeterinarianReportEntry(veterinarianTwo, "Dr. Beto", "EN_PROGRESO"),
                new AppointmentVeterinarianReportEntry(veterinarianTwo, "Dr. Beto", "NO_ASISTIO"),
                new AppointmentVeterinarianReportEntry(veterinarianTwo, "Dr. Beto", "ATENDIDA")
            });
        var handler = new GetAppointmentsByVeterinarianReportQueryHandler(unitOfWork);

        var result = await handler.Handle(
            new GetAppointmentsByVeterinarianReportQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)),
            CancellationToken.None);

        var reports = result.ToArray();
        Assert.Equal(2, reports.Length);
        Assert.Equal(veterinarianTwo, reports[0].VeterinarianId);
        Assert.Equal(4, reports[0].TotalAppointments);
        Assert.Equal(1, reports[0].AttendedCount);
        Assert.Equal(0, reports[0].CanceledCount);
        Assert.Equal(2, reports[0].ScheduledCount);
        Assert.Equal(1, reports[0].OtherCount);
        Assert.Equal(veterinarianOne, reports[1].VeterinarianId);
        Assert.Equal(3, reports[1].TotalAppointments);
        Assert.Equal(1, reports[1].AttendedCount);
        Assert.Equal(1, reports[1].CanceledCount);
        Assert.Equal(1, reports[1].ScheduledCount);
        Assert.Equal(0, reports[1].OtherCount);
        await appointmentsRepository.Received(1).GetForVeterinarianReportAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);
    }
}
