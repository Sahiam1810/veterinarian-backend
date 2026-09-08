using Application.Reports.UseCases;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Reports;

public sealed class GetAppointmentsByStatusReportQueryValidatorTests
{
    private readonly GetAppointmentsByStatusReportQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidDateRange_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var query = new GetAppointmentsByStatusReportQuery(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FromAfterTo_ShouldHaveValidationError()
    {
        // Arrange
        var query = new GetAppointmentsByStatusReportQuery(
            new DateOnly(2026, 9, 30),
            new DateOnly(2026, 9, 1));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("La fecha inicial 'from' no puede ser posterior a la fecha final 'to'.");
    }

    [Fact]
    public void Validate_SameFromAndToDate_ShouldBeValid()
    {
        // Arrange
        var query = new GetAppointmentsByStatusReportQuery(
            new DateOnly(2026, 9, 15),
            new DateOnly(2026, 9, 15));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
