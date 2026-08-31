using System.Net.Http.Json;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Infrastructure.Telegram.Configuration;
using Infrastructure.Telegram.Http.Contracts;
using Microsoft.Extensions.Options;

namespace Infrastructure.Telegram.Http;

public sealed class TelegramBotHttpClient(
    HttpClient httpClient,
    IOptions<TelegramOptions> options) : ITelegramBotClient
{
    private readonly string botToken = options.Value.BotToken;

    public async Task<long> SendTextAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"bot{botToken}/sendMessage",
                new TelegramSendMessageRequest(chatId, text),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new TelegramDeliveryException();
            }

            var body = await response.Content.ReadFromJsonAsync<TelegramSendMessageResponse>(
                cancellationToken: cancellationToken);
            if (body is not { Ok: true, Result: not null })
            {
                throw new TelegramDeliveryException();
            }

            return body.Result.MessageId;
        }
        catch (TelegramDeliveryException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new TelegramDeliveryException(exception);
        }
    }
}
