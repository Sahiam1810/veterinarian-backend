using Application.Reports.UseCases;
using Xunit;

namespace Application.Tests.Reports;

public sealed class GetAppointmentsByDayReportQueryValidatorTests
{
    private readonly GetAppointmentsByDayReportQueryValidator sut = new();

    [Fact]
    public void Validate_fails_when_from_is_after_to()
    {
        var result = sut.Validate(
            new GetAppointmentsByDayReportQuery(new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_succeeds_when_from_equals_to()
    {
        var result = sut.Validate(
            new GetAppointmentsByDayReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_succeeds_when_from_is_before_to()
    {
        var result = sut.Validate(
            new GetAppointmentsByDayReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)));

        Assert.True(result.IsValid);
    }
}
