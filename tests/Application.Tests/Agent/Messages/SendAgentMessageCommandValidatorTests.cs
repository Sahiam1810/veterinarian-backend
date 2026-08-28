using Application.Agent.Messages;
using Xunit;

namespace Application.Tests.Agent.Messages;

public sealed class SendAgentMessageCommandValidatorTests
{
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CorrelationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly SendAgentMessageCommandValidator validator = new();

    public static TheoryData<SendAgentMessageCommand, string> InvalidCommands => new()
    {
        { Valid() with { Message = string.Empty }, nameof(SendAgentMessageCommand.Message) },
        { Valid() with { Message = new string('m', 8001) }, nameof(SendAgentMessageCommand.Message) },
        { Valid() with { Language = string.Empty }, nameof(SendAgentMessageCommand.Language) },
        { Valid() with { Language = new string('l', 21) }, nameof(SendAgentMessageCommand.Language) },
        { Valid() with { PersonId = Guid.Empty }, nameof(SendAgentMessageCommand.PersonId) },
        { Valid() with { Role = " " }, nameof(SendAgentMessageCommand.Role) },
        { Valid() with { Role = new string('r', 81) }, nameof(SendAgentMessageCommand.Role) },
        { Valid() with { IdempotencyKey = " " }, nameof(SendAgentMessageCommand.IdempotencyKey) },
        { Valid() with { IdempotencyKey = new string('i', 161) }, nameof(SendAgentMessageCommand.IdempotencyKey) },
        { Valid() with { CorrelationId = Guid.Empty }, nameof(SendAgentMessageCommand.CorrelationId) }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Validate_rejects_invalid_command_property(
        SendAgentMessageCommand command,
        string propertyName)
    {
        var result = await validator.ValidateAsync(command);

        Assert.Contains(result.Errors, failure => failure.PropertyName == propertyName);
    }

    [Fact]
    public async Task Validate_accepts_complete_command()
    {
        var result = await validator.ValidateAsync(Valid());

        Assert.True(result.IsValid);
    }

    private static SendAgentMessageCommand Valid() =>
        new(
            "Necesito información sobre vacunas",
            null,
            null,
            "es-CO",
            PersonId,
            "Cliente",
            "message-001",
            CorrelationId);
}
