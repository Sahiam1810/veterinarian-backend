using Infrastructure.Agent.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Agent.Configuration;

public sealed class AgentOptionsValidatorTests
{
    private readonly AgentOptionsValidator validator = new();

    [Fact]
    public void Disabled_options_accept_empty_address()
    {
        var result = validator.Validate(null, new AgentOptions
        {
            Enabled = false,
            BaseUrl = string.Empty
        });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("agent-api:8000")]
    [InlineData("ftp://agent-api")]
    [InlineData("http://user:secret@agent-api")]
    [InlineData("http://agent-api?debug=true")]
    [InlineData("http://agent-api#fragment")]
    public void Enabled_options_require_absolute_safe_http_or_https_base_url(string baseUrl)
    {
        var result = validator.Validate(null, Valid(baseUrl: baseUrl));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Agent:BaseUrl", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("api/v1/messages")]
    [InlineData("https://agent-api/api/v1/messages")]
    [InlineData("/api/../messages")]
    [InlineData("/api/v1/messages?debug=true")]
    [InlineData("/api/v1/messages#fragment")]
    public void Enabled_options_require_safe_relative_messages_path(string messagesPath)
    {
        var result = validator.Validate(null, Valid(messagesPath: messagesPath));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Agent:MessagesPath", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void Enabled_options_require_timeout_between_1_and_120_seconds(int timeout)
    {
        var result = validator.Validate(null, Valid(requestTimeoutSeconds: timeout));

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData(1023)]
    [InlineData(1048577)]
    public void Enabled_options_require_max_response_bytes_between_1024_and_1048576(int maxBytes)
    {
        var result = validator.Validate(null, Valid(maxResponseBytes: maxBytes));

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Enabled_options_require_valid_initial_conversation_status_id(string value)
    {
        var result = validator.Validate(null, Valid(initialConversationStatusId: value));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "Agent:InitialConversationStatusId",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Enabled_options_require_valid_client_participant_type_id(string value)
    {
        var result = validator.Validate(null, Valid(clientParticipantTypeId: value));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "Agent:ClientParticipantTypeId",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_options_accept_valid_configuration()
    {
        var result = validator.Validate(null, Valid());

        Assert.True(result.Succeeded);
    }

    private static AgentOptions Valid(
        string baseUrl = "https://agent-api:8000",
        string messagesPath = "/api/v1/messages",
        int requestTimeoutSeconds = 30,
        int maxResponseBytes = 1_048_576,
        string initialConversationStatusId = "81000000-0000-0000-0000-000000000001",
        string clientParticipantTypeId = "82000000-0000-0000-0000-000000000001") => new()
    {
        Enabled = true,
        BaseUrl = baseUrl,
        MessagesPath = messagesPath,
        RequestTimeoutSeconds = requestTimeoutSeconds,
        MaxResponseBytes = maxResponseBytes,
        InitialConversationStatusId = initialConversationStatusId,
        ClientParticipantTypeId = clientParticipantTypeId
    };
}
