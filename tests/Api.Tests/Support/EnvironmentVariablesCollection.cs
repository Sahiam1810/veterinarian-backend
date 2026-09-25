using Xunit;

namespace Api.Tests.Support;

// Serializa tests cuyas factories mutan Environment.SetEnvironmentVariable.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection
{
    public const string Name = "EnvironmentVariables";
}
