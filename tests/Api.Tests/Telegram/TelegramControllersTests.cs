using System.Security.Claims;
using Api.Telegram.Controllers;
using Api.Telegram.Dtos;
using Api.Telegram.Security;
using Application.Telegram.Linking;
using Application.Telegram.Updates;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Telegram;

public sealed class TelegramControllersTests
{
    [Fact]
    public async Task Link_code_uses_authenticated_person_claim()
    {
        var personId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CreateTelegramLinkCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(new TelegramLinkCodeResult("code", "https://t.me/bot?start=code", DateTimeOffset.UtcNow));
        var controller = new TelegramLinkCodesController(sender)
        {
            ControllerContext = ContextWithClaim("person_id", personId.ToString())
        };

        var result = await controller.Create(default);

        Assert.IsType<ObjectResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<CreateTelegramLinkCodeCommand>(command => command.PersonId == personId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Webhook_rejects_invalid_secret_without_enqueueing()
    {
        var sender = Substitute.For<ISender>();
        var validator = Substitute.For<ITelegramWebhookSecretValidator>();
        validator.IsValid("wrong").Returns(false);
        var controller = new TelegramWebhookController(sender, validator);

        var result = await controller.Receive(Update(), "wrong", default);

        Assert.IsType<UnauthorizedResult>(result);
        await sender.DidNotReceive().Send(
            Arg.Any<IngestTelegramUpdateCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Webhook_maps_private_text_message_to_ingestion()
    {
        var sender = Substitute.For<ISender>();
        var validator = Substitute.For<ITelegramWebhookSecretValidator>();
        validator.IsValid("valid").Returns(true);
        var controller = new TelegramWebhookController(sender, validator);

        var result = await controller.Receive(Update(), "valid", default);

        Assert.IsType<OkResult>(result);
        await sender.Received(1).Send(
            Arg.Is<IngestTelegramUpdateCommand>(command =>
                command.UpdateId == 42 && command.TelegramChatId == 1001 && command.Text == "hola"),
            Arg.Any<CancellationToken>());
    }

    private static TelegramUpdateRequest Update() =>
        new(42, new TelegramMessageRequest(
            7,
            new TelegramFromRequest(1001),
            new TelegramChatRequest(1001, "private"),
            "hola"));

    private static ControllerContext ContextWithClaim(string type, string value) =>
        new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(type, value)], "test"))
            }
        };
}
