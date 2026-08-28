using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Infrastructure.Agent.Configuration;
using Infrastructure.Agent.Http.Contracts;
using Microsoft.Extensions.Options;

namespace Infrastructure.Agent.Http;

public sealed class AgentMessagingHttpClient(
    HttpClient httpClient,
    IOptions<AgentOptions> options) : IAgentMessagingClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly AgentOptions agentOptions = options.Value;

    public async Task<AgentMessageResult> SendAsync(
        AgentMessageEnvelope message,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, agentOptions.MessagesPath)
            {
                Content = JsonContent.Create(ToHttpRequest(message), options: SerializerOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            ThrowForStatus(response.StatusCode);

            var payload = await ReadSuccessAsync(response.Content, cancellationToken);
            if (payload.ConversationId != message.ConversationId ||
                payload.CorrelationId != message.CorrelationId ||
                string.IsNullOrWhiteSpace(payload.ResponseType))
            {
                throw new AgentContractException();
            }

            return new AgentMessageResult(
                payload.Message,
                payload.ConversationId,
                payload.CorrelationId,
                payload.ResponseType,
                payload.Module);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw new AgentTimeoutException(exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AgentUnavailableException(exception);
        }
        catch (JsonException exception)
        {
            throw new AgentContractException(exception);
        }
    }

    private static AgentHttpRequest ToHttpRequest(AgentMessageEnvelope message) =>
        new(
            message.Message,
            message.ConversationId,
            message.UserId,
            message.PetId,
            message.Channel,
            message.Language,
            message.Roles,
            message.IsEscalated,
            message.CorrelationId,
            message.IdempotencyKey,
            message.PublishAsGlobalKnowledge);

    private async Task<AgentHttpResponse> ReadSuccessAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        var declaredLength = content.Headers.ContentLength;
        if (declaredLength is > 0 && declaredLength.Value > agentOptions.MaxResponseBytes)
        {
            throw new AgentContractException();
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        await using var bounded = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
        {
            if (bounded.Length + read > agentOptions.MaxResponseBytes)
            {
                throw new AgentContractException();
            }

            await bounded.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        bounded.Position = 0;
        return await JsonSerializer.DeserializeAsync<AgentHttpResponse>(
                   bounded,
                   SerializerOptions,
                   cancellationToken)
               ?? throw new AgentContractException();
    }

    private static void ThrowForStatus(HttpStatusCode statusCode)
    {
        if ((int)statusCode is >= 200 and <= 299)
        {
            return;
        }

        throw statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new AgentAuthenticationException(),
            HttpStatusCode.Conflict => new AgentIdempotencyConflictException(),
            HttpStatusCode.GatewayTimeout => new AgentTimeoutException(),
            HttpStatusCode.TooManyRequests or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable => new AgentUnavailableException(),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                new AgentContractException(),
            _ when (int)statusCode >= 500 => new AgentUnavailableException(),
            _ => new AgentContractException()
        };
    }
}
