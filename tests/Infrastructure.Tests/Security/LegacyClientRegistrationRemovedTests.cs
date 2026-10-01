using Infrastructure.Security.Authentication;
using Xunit;

namespace Infrastructure.Tests.Security;

// Tarea 4.4: sin servicio legacy ni registro DI que cree credentials de Cliente con password.
public sealed class LegacyClientRegistrationRemovedTests
{
    [Fact]
    public void Infrastructure_does_not_define_ClientAccountRegistrationService()
    {
        var leftovers = typeof(AuthenticationService).Assembly
            .GetTypes()
            .Where(type => type.Name.Contains("ClientAccountRegistration", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(leftovers);
    }

    [Fact]
    public void DependencyInjection_source_does_not_register_ClientAccountRegistration()
    {
        // Regresión de texto: si alguien vuelve a AddScoped el legacy, este test falla en CI.
        var diPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Infrastructure", "DependencyInjection.cs"));
        Assert.True(File.Exists(diPath), $"No se encontró DependencyInjection.cs en {diPath}");
        var source = File.ReadAllText(diPath);
        Assert.DoesNotContain("IClientAccountRegistrationService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ClientAccountRegistrationService", source, StringComparison.Ordinal);
    }
}
