using Xunit;

namespace Api.Tests.Security;

public sealed class ReportesPermissionsSeedTests
{
    [Fact]
    public void Modules_seed_defines_Reportes_idempotently()
    {
        var sql = File.ReadAllText(FindSeedPath("modules_seed.sql"));

        Assert.Contains("UPPER('Reportes')", sql, StringComparison.Ordinal);
        Assert.Contains("WHEN NOT MATCHED THEN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Reportes operativos y de gestión'", sql, StringComparison.Ordinal);
        Assert.False(
            File.ReadAllBytes(FindSeedPath("modules_seed.sql")).AsSpan()
                .StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    [Fact]
    public void Role_permissions_seed_grants_Reportes_View_only_to_Administrador()
    {
        var sql = File.ReadAllText(FindSeedPath("role_permissions_seed.sql"));

        Assert.Contains(
            "ensure_permission('d1e3a202-1e6c-4a86-94c3-289de0ca7c21', '11111111-1111-1111-1111-111111111111', 'Reportes', 1, 0, 0, 0)",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("WHEN NOT MATCHED THEN", sql, StringComparison.OrdinalIgnoreCase);

        var reportesAssignments = sql
            .Split('\n')
            .Where(line => line.Contains("'Reportes'", StringComparison.Ordinal))
            .ToArray();

        Assert.Single(reportesAssignments);
        Assert.Contains("11111111-1111-1111-1111-111111111111", reportesAssignments[0]);
        Assert.DoesNotContain("'44444444-4444-4444-4444-444444444444', 'Reportes'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("'55555555-5555-5555-5555-555555555555', 'Reportes'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("'66666666-6666-6666-6666-666666666666', 'Reportes'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("'77777777-7777-7777-7777-777777777777', 'Reportes'", sql, StringComparison.Ordinal);
    }

    private static string FindSeedPath(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"No se encontro database/seeds/{fileName}");
    }
}
