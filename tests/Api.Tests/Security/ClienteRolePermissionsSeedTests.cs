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

        // Regla documentada: borrar residuales; no reinsertar permisos de plataforma.
        Assert.Contains("DELETE FROM ROLE_PERMISSIONS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("77777777-7777-7777-7777-777777777777", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_seeds_use_utf8_without_bom_and_share_the_same_module_name()
    {
        var permissionPath = FindSeedPath();
        var seedDirectory = Path.GetDirectoryName(permissionPath)!;
        var modulePath = Path.Combine(seedDirectory, "modules_seed.sql");
        var verificationPath = Path.Combine(seedDirectory, "verify_seeds.sql");
        var permissionSql = File.ReadAllText(permissionPath);
        var moduleSql = File.ReadAllText(modulePath);

        foreach (var path in new[] { permissionPath, modulePath, verificationPath })
        {
            Assert.False(
                File.ReadAllBytes(path).AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }),
                $"SQL*Plus interpreta el BOM de {Path.GetFileName(path)} como parte del primer comando.");
        }

        Assert.Contains("'Historiales Clínicos'", permissionSql, StringComparison.Ordinal);
        Assert.Contains("'Historiales Clínicos'", moduleSql, StringComparison.Ordinal);
    }

    private static string FindSeedPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", "extra", "role_permissions_seed.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException("No se encontro database/seeds/extra/role_permissions_seed.sql");
    }
}
