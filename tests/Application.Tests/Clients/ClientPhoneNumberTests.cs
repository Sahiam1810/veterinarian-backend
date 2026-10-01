using Domain.Clients.ValueObjects;
using Xunit;

namespace Application.Tests.Clients;

public sealed class ClientPhoneNumberTests
{
    [Fact]
    public void Create_normalizes_to_digits_only()
    {
        var phone = ClientPhoneNumber.Create("+57 (300) 123-4567");

        Assert.Equal("573001234567", phone.Value);
    }

    [Fact]
    public void Create_rejects_blank_values()
    {
        Assert.Throws<ArgumentException>(() => ClientPhoneNumber.Create("  "));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789012345678901")]
    public void Create_rejects_invalid_digit_lengths(string phone)
    {
        Assert.Throws<ArgumentException>(() => ClientPhoneNumber.Create(phone));
    }

    [Fact]
    public void TryCreate_rejects_alphanumeric_input_that_does_not_yield_enough_digits()
    {
        Assert.False(ClientPhoneNumber.TryCreate("abc12", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromPersistence_returns_null_for_blank(string? value)
    {
        Assert.Null(ClientPhoneNumber.CreateFromPersistence(value));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789012345678901")]
    public void CreateFromPersistence_returns_null_for_invalid_historical_values(string value)
    {
        Assert.Null(ClientPhoneNumber.CreateFromPersistence(value));
    }

    [Fact]
    public void CreateFromPersistence_returns_normalized_value_for_valid_phone()
    {
        var phone = ClientPhoneNumber.CreateFromPersistence("+57 (300) 123-4567");

        Assert.NotNull(phone);
        Assert.Equal("573001234567", phone!.Value);
    }
}
