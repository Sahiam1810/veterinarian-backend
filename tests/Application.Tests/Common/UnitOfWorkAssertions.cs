using Application.Common.Abstractions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Common;

internal static class UnitOfWorkAssertions
{
    // Nombres de los repositorios (propiedades *Repository) que el código bajo prueba pidió al UnitOfWork.
    public static IReadOnlyCollection<string> RepositoriesAccessed(IUnitOfWork unitOfWork) =>
        unitOfWork.ReceivedCalls()
            .Select(call => call.GetMethodInfo().Name)
            .Where(name => name.StartsWith("get_", StringComparison.Ordinal) && name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(name => name["get_".Length..])
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    // Falla si se tocó cualquier repositorio fuera de los permitidos (p. ej. USERS o USER_ACCOUNTS).
    public static void AssertOnlyRepositoriesAccessed(IUnitOfWork unitOfWork, params string[] allowed)
    {
        var unexpected = RepositoriesAccessed(unitOfWork).Except(allowed).ToArray();
        Assert.True(
            unexpected.Length == 0,
            $"Se accedió a repositorios no permitidos: {string.Join(", ", unexpected)}.");
    }
}
