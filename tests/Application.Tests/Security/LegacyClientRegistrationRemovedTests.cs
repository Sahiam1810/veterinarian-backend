using Application.Common.Abstractions;
using Xunit;

namespace Application.Tests.Security;

// Tarea 4.4: el puerto legacy ClientAccountRegistration no debe volver a Application.
public sealed class LegacyClientRegistrationRemovedTests
{
    [Fact]
    public void Application_does_not_define_ClientAccountRegistration_types()
    {
        var leftovers = typeof(IUnitOfWork).Assembly
            .GetTypes()
            .Where(type => type.FullName is not null
                && type.FullName.Contains("ClientAccountRegistration", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(leftovers);
    }
}
