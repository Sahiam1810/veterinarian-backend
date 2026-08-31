using Domain.ChatEscalationResolutions.Entities;
using Domain.ChatEscalations.Entities;
using Infrastructure.Agent.Conversations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Agent.Conversations;

public sealed class ActiveConversationEscalationReaderTests
{
    private static readonly Guid ConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EscalationStatusId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task HasActive_returns_true_when_an_escalation_has_no_resolution()
    {
        await using var context = CreateContext();
        var escalation = ChatEscalation.Create(ConversationId, EscalationStatusId, true);
        context.Add(escalation);
        await context.SaveChangesAsync();
        var reader = new ActiveConversationEscalationReader(context);

        var result = await reader.HasActiveAsync(ConversationId, default);

        Assert.True(result);
    }

    [Fact]
    public async Task HasActive_returns_false_when_every_escalation_is_resolved()
    {
        await using var context = CreateContext();
        var escalation = ChatEscalation.Create(ConversationId, EscalationStatusId, true);
        var resolution = ChatEscalationResolution.Create(escalation.Id);
        context.AddRange(escalation, resolution);
        await context.SaveChangesAsync();
        var reader = new ActiveConversationEscalationReader(context);

        var result = await reader.HasActiveAsync(ConversationId, default);

        Assert.False(result);
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }
}
