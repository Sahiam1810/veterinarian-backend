using System.Reflection;
using Api.AccountStatements.Controllers;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.AccountStatements;

public sealed class AccountStatementsMineGoneTests
{
    [Fact]
    public void GetMine_is_allow_anonymous()
    {
        var method = typeof(AccountStatementsController).GetMethod(nameof(AccountStatementsController.GetMine));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task GetMine_throws_ClientPortalGone()
    {
        var controller = new AccountStatementsController(Substitute.For<ISender>());
        var ex = await Assert.ThrowsAsync<GoneException>(() => controller.GetMine(CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}