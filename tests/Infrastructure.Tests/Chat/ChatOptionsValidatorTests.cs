using Infrastructure.Chat.Configuration;
using Xunit;

namespace Infrastructure.Tests.Chat;

public sealed class ChatOptionsValidatorTests
{
    [Fact]
    public void Valid_options_pass_validation()
    {
        var options = new ChatOptions
        {
            ResolvedEscalationStatusId = "85000000-0000-0000-0000-000000000004",
        };

        var result = new ChatOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Resolved_escalation_status_id_must_be_a_non_empty_guid(string invalidId)
    {
        var options = new ChatOptions { ResolvedEscalationStatusId = invalidId };

        var result = new ChatOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }
}
