using System.Reflection;
using Api.Clients.Controllers;
using Xunit;

namespace Api.Tests.Clients;

// Contrato v2: el cliente no tiene usuario, así que desaparecen las rutas que
// dependían de uno (register-owner, owner-profile y me).
public sealed class ClientsControllerSurfaceTests
{
    [Theory]
    [InlineData("RegisterOwner")]
    [InlineData("UpdateOwnerProfile")]
    [InlineData("GetMe")]
    public void Retired_routes_no_longer_exist(string methodName)
    {
        var method = typeof(ClientsController).GetMethod(methodName);

        Assert.Null(method);
    }

    [Fact]
    public void Controller_only_depends_on_the_mediator()
    {
        var constructor = Assert.Single(typeof(ClientsController).GetConstructors());

        Assert.Single(constructor.GetParameters());
    }
}
