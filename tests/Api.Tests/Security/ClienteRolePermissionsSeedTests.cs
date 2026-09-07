using System.Text.RegularExpressions;
using Xunit;

namespace Api.Tests.Security;

// Etapa 5.3: el seed no otorga View/Create/Edit/Delete de modulos al rol Cliente.
public sealed class ClienteRolePermissionsSeedTests
{
    private static readonly Guid ClienteRoleId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public void Role_permissions_seed_does_not_grant_modules_to_Cliente()
    {
        var seedPath = FindSeedPath();
        var sql = File.ReadAllText(seedPath);

        // ensure_permission(id, roleId, module, canView, canCreate, canEdit, canDelete)
        var matches = Regex.Matches(
            sql,
            @"ensure_permission\s*\(\s*'[^']+'\s*,\s*'([^']+)'\s*,",
            RegexOptions.IgnoreCase);

        Assert.NotEmpty(matches);
        foreach (Match match in matches)
        {
            var roleId = Guid.Parse(match.Groups[1].Value);
            Assert.NotEqual(ClienteRoleId, roleId);
        }

        Assert.Contains("No se asignan filas en ROLE_PERMISSIONS", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSeedPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", "role_permissions_seed.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException("No se encontro database/seeds/role_permissions_seed.sql");
    }
}