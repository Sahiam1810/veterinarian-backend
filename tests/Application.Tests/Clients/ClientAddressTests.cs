using Domain.Clients.ValueObjects;
using Xunit;

namespace Application.Tests.Clients;

public sealed class ClientAddressTests
{
    [Fact]
    public void Create_accepts_address_up_to_150_characters()
    {
        var address100 = new string('A', 100);
        var address150 = new string('B', 150);

        var clientAddress100 = ClientAddress.Create(address100);
        var clientAddress150 = ClientAddress.Create(address150);

        Assert.Equal(address100, clientAddress100.Value);
        Assert.Equal(address150, clientAddress150.Value);
    }

    [Fact]
    public void Create_throws_ArgumentException_when_address_exceeds_150_characters()
    {
        var address151 = new string('C', 151);

        var exception = Assert.Throws<ArgumentException>(() => ClientAddress.Create(address151));

        Assert.Contains("La dirección no puede superar los 150 caracteres.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_handles_null_or_whitespace(string? input)
    {
        var address = ClientAddress.Create(input);

        Assert.Null(address.Value);
    }
}
