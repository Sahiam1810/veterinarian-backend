using System.Reflection;
using Api.Clients.Controllers;
using Application.Common.Exceptions;
using Application.Owners.Abstractions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Clients;

// Etapa 5.2a: GET /api/clients/me responde Gone (sin JWT ClientOnly).
public sealed class ClientsMeGoneTests
{
    [Fact]
    public void GetMe_is_allow_anonymous_and_not_client_only()
    {
        var method = typeof(ClientsController).GetMethod(nameof(ClientsController.GetMe));
        Assert.NotNull(method);

        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
        Assert.DoesNotContain(authorize, a => a.Policy == Api.Common.Security.AuthorizationPolicies.ClientOnly);
    }

    [Fact]
    public async Task GetMe_throws_GoneException_with_ClientPortalGone()
    {
        var controller = new ClientsController(Substitute.For<ISender>(), Substitute.For<IRegisterOwnerFromStaff>());

        var ex = await Assert.ThrowsAsync<GoneException>(() => controller.GetMe(CancellationToken.None));

        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}