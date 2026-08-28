using Infrastructure.Agent.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests.Agent;

public sealed class AgentOptionsStartupTests
{
    [Fact]
    public void Disabled_agent_does_not_require_base_url()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Agent:Enabled"] = "false"
        });

        var options = provider.GetRequiredService<IOptions<AgentOptions>>().Value;

        Assert.False(options.Enabled);
    }

    [Fact]
    public void Enabled_agent_with_empty_base_url_fails_validation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Agent:Enabled"] = "true",
            ["Agent:BaseUrl"] = ""
        });

        var exception = Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<AgentOptions>>().Value);

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains("Agent:BaseUrl", StringComparison.Ordinal));
    }

    private static ServiceProvider BuildProvider(
        Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IValidateOptions<AgentOptions>, AgentOptionsValidator>();
        services.AddOptions<AgentOptions>()
            .Bind(configuration.GetSection(AgentOptions.SectionName))
            .ValidateOnStart();
        return services.BuildServiceProvider();
    }
}
