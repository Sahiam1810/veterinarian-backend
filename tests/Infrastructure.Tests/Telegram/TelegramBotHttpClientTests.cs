using System.Net;
using System.Text;
using Application.Telegram.Errors;
using Infrastructure.Telegram.Configuration;
using Infrastructure.Telegram.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramBotHttpClientTests
{
    [Fact]
    public async Task Send_text_posts_contract_and_returns_message_id()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"ok\":true,\"result\":{\"message_id\":77}}",
                Encoding.UTF8,
                "application/json")
        });
        var client = CreateClient(handler);

        var messageId = await client.SendTextAsync(1001, "hola", default);

        Assert.Equal(77, messageId);
        Assert.Contains("botsecret/sendMessage", handler.Request!.RequestUri!.ToString());
        Assert.Contains("\"chat_id\":1001", handler.Body);
        Assert.Contains("\"text\":\"hola\"", handler.Body);
    }

    [Fact]
    public async Task Server_failure_throws_safe_delivery_exception()
    {
        var client = CreateClient(new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var exception = await Assert.ThrowsAsync<TelegramDeliveryException>(
            () => client.SendTextAsync(1001, "hola", default));

        Assert.DoesNotContain("secret", exception.Message);
    }

    [Fact]
    public async Task Token_separator_is_kept_inside_the_https_request_path()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"ok\":true,\"result\":{\"message_id\":78}}",
                Encoding.UTF8,
                "application/json")
        });
        var client = CreateClient(handler, "123456:token-secret");

        await client.SendTextAsync(1001, "hola", default);

        Assert.Equal(
            "https://api.telegram.org/bot123456:token-secret/sendMessage",
            handler.Request!.RequestUri!.AbsoluteUri);
    }

    private static TelegramBotHttpClient CreateClient(
        HttpMessageHandler handler,
        string token = "secret") =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") },
            Options.Create(new TelegramOptions { Enabled = true, BotToken = token }));

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
