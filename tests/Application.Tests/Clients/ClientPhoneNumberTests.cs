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

    [Fact]
    public void TryCreate_rejects_alphanumeric_input_that_does_not_yield_enough_digits()
    {
        Assert.False(ClientPhoneNumber.TryCreate("abc12", out _));
    }
}
