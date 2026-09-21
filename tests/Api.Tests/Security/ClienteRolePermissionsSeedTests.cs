using Xunit;

namespace Api.Tests.Security;

// T10: el rol Cliente ya no se siembra en seeds extra; no debe aparecer el GUID ni DELETE residuales.
public sealed class ClienteRolePermissionsSeedTests
{
    private const string ClienteRoleId = "77777777-7777-7777-7777-777777777777";

    [Fact]
    public void Role_permissions_seed_does_not_reference_Cliente_role_id()
    {
        var seedPath = FindSeedPath();
        var sql = File.ReadAllText(seedPath);

        Assert.DoesNotContain(ClienteRoleId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM ROLE_PERMISSIONS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_seeds_use_utf8_without_bom_and_share_the_same_module_name()
    {
        var permissionPath = FindSeedPath();
        var seedDirectory = Path.GetDirectoryName(permissionPath)!;
        var modulePath = Path.Combine(seedDirectory, "modules_seed.sql");
        var verificationPath = File.Exists(Path.Combine(seedDirectory, "verify_seeds.sql"))
            ? Path.Combine(seedDirectory, "verify_seeds.sql")
            : Path.Combine(Directory.GetParent(seedDirectory)!.FullName, "verify_seeds.sql");
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
        Assert.DoesNotContain(ClienteRoleId, File.ReadAllText(verificationPath), StringComparison.OrdinalIgnoreCase);
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
