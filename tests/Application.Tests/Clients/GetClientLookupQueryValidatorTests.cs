using Application.Clients.UseCases;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Clients;

public sealed class GetClientLookupQueryValidatorTests
{
    private readonly GetClientLookupQueryValidator validator = new();

    [Fact]
    public void Should_have_error_when_both_identification_and_phone_are_null_or_empty()
    {
        var query = new GetClientLookupQuery(null, null);

        var result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_not_have_error_when_identification_is_provided()
    {
        var query = new GetClientLookupQuery("1234567890", null);

        var result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_not_have_error_when_phone_is_provided()
    {
        var query = new GetClientLookupQuery(null, "3001234567");

        var result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_not_have_error_when_both_are_provided()
    {
        var query = new GetClientLookupQuery("1234567890", "3001234567");

        var result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_phone_has_too_few_digits()
    {
        var query = new GetClientLookupQuery(null, "123");

        var result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Should_have_error_when_identification_exceeds_max_length()
    {
        var query = new GetClientLookupQuery(new string('1', 21), null);

        var result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber);
    }
}
