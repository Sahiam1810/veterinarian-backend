using Application.Agent.Abstractions;
using Application.Agent.Conversations;
using Application.Agent.Errors;
using Infrastructure.Agent.Conversations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Tests.Agent;

public sealed class AgentDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_registers_persistent_conversation_services_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(Configuration());

        var providerDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IConversationContextProvider));
        Assert.Equal(ServiceLifetime.Scoped, providerDescriptor.Lifetime);
        Assert.Equal(
            typeof(PersistentConversationContextProvider),
            providerDescriptor.ImplementationType);
        var escalationDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType ==
                typeof(IActiveConversationEscalationReader));
        Assert.Equal(ServiceLifetime.Scoped, escalationDescriptor.Lifetime);
        Assert.Equal(
            typeof(ActiveConversationEscalationReader),
            escalationDescriptor.ImplementationType);
    }

    [Fact]
    public void AddInfrastructure_resolves_configured_conversation_defaults()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(Configuration());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var defaults = scope.ServiceProvider
            .GetRequiredService<IAgentConversationDefaults>();

        Assert.Equal(
            Guid.Parse("81000000-0000-0000-0000-000000000001"),
            defaults.InitialConversationStatusId);
        Assert.Equal(
            Guid.Parse("82000000-0000-0000-0000-000000000001"),
            defaults.ClientParticipantTypeId);
    }

    [Fact]
    public async Task AddInfrastructure_disabled_agent_resolves_an_unavailable_context()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(Configuration(enabled: false));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        await Assert.ThrowsAsync<AgentUnavailableException>(async () =>
        {
            var context = scope.ServiceProvider
                .GetRequiredService<IConversationContextProvider>();
            await context.ResolveAsync(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                null,
                "message-disabled",
                default);
        });
    }

    private static IConfiguration Configuration(bool enabled = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent:Enabled"] = enabled.ToString()
        };
        if (enabled)
        {
            values["Agent:BaseUrl"] = "https://agent-api.test";
            values["Agent:MessagesPath"] = "/api/v1/messages";
            values["Agent:InitialConversationStatusId"] =
                "81000000-0000-0000-0000-000000000001";
            values["Agent:ClientParticipantTypeId"] =
                "82000000-0000-0000-0000-000000000001";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
