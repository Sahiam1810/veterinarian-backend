using Api.Telegram.Controllers;
using Api.Telegram.Dtos;
using Application.Common.Results;
using Application.Telegram.Registration;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Telegram;

public sealed class TelegramRegistrationControllerTests
{
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly IWebHostEnvironment environment = Substitute.For<IWebHostEnvironment>();

    [Fact]
    public async Task Token_query_is_exchanged_for_http_only_cookie_and_clean_redirect()
    {
        environment.EnvironmentName.Returns("Development");
        sender.Send(Arg.Any<GetTelegramRegistrationSessionQuery>(), default)
            .Returns(Result<PendingTelegramRegistration>.Success(
                new PendingTelegramRegistration(DateTime.UtcNow.AddMinutes(15))));
        var controller = CreateController();

        var result = await controller.Complete("raw-token", default);

        Assert.IsType<RedirectToActionResult>(result);
        var cookie = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_or_expired_cookie_returns_gone_view()
    {
        var controller = CreateController();

        var result = await controller.Complete(null, default);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, view.StatusCode);
        Assert.Equal("Expired", view.ViewName);
    }

    [Fact]
    public async Task Successful_post_renders_confirmation_without_tokens()
    {
        var controller = CreateController("raw-token");
        sender.Send(Arg.Any<CompleteTelegramRegistrationCommand>(), default)
            .Returns(Result<CompletedTelegramRegistration>.Success(
                new CompletedTelegramRegistration(Guid.NewGuid(), 1001)));
        var request = new CompleteTelegramRegistrationRequest(
            "Ana Cliente", "1234567890", "ana.cliente",
            "Password123!", "Password123!");

        var result = await controller.Submit(request, default);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Success", view.ViewName);
        Assert.Null(view.Model);
    }

    private TelegramRegistrationController CreateController(string? cookieToken = null)
    {
        environment.EnvironmentName.Returns("Development");
        var context = new DefaultHttpContext();
        if (cookieToken is not null)
        {
            context.Request.Headers.Cookie = $"HuellitasTelegramRegistration={cookieToken}";
        }

        return new TelegramRegistrationController(sender, environment)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }
}
