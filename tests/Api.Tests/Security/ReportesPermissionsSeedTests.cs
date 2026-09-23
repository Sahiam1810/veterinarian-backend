using Xunit;

namespace Api.Tests.Security;

public sealed class ReportesPermissionsSeedTests
{
    private const string AdminReportes =
        "'d1e3a202-1e6c-4a86-94c3-289de0ca7c21', '11111111-1111-1111-1111-111111111111', 'Reportes', 1, 0, 0, 0)";

    private const string RecepcionistaReportes =
        "'b8c1d4e7-5a2f-4e91-9c3b-7d6e8f0a1b2c', '55555555-5555-5555-5555-555555555555', 'Reportes', 1, 0, 0, 0)";

    [Fact]
    public void Modules_seed_defines_Reportes_idempotently()
    {
        var sql = File.ReadAllText(FindExtraSeedPath("modules_seed.sql"));

        Assert.Contains("UPPER('Reportes')", sql, StringComparison.Ordinal);
        Assert.Contains("WHEN NOT MATCHED THEN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Reportes operativos y de gestión'", sql, StringComparison.Ordinal);
        Assert.False(
            File.ReadAllBytes(FindExtraSeedPath("modules_seed.sql")).AsSpan()
                .StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    [Fact]
    public void Role_permissions_seed_grants_Reportes_View_only_to_Administrador_and_Recepcionista()
    {
        AssertReportesViewOnlyAssignments(File.ReadAllText(FindExtraSeedPath("role_permissions_seed.sql")));
    }

    [Fact]
    public void Insert_all_seeds_grants_Reportes_View_only_to_Administrador_and_Recepcionista()
    {
        AssertReportesViewOnlyAssignments(File.ReadAllText(FindRootSeedPath("insert_all_seeds.sql")));
    }

    [Fact]
    public void Repair_seed_grants_Reportes_View_only_to_Administrador_and_Recepcionista()
    {
        AssertReportesViewOnlyAssignments(
            File.ReadAllText(FindExtraSeedPath("role_permissions_repair_2026-09-10.sql")));
    }

    private static void AssertReportesViewOnlyAssignments(string sql)
    {
        Assert.Contains(AdminReportes, sql, StringComparison.Ordinal);
        Assert.Contains(RecepcionistaReportes, sql, StringComparison.Ordinal);

        var reportesAssignments = sql
            .Split('\n')
            .Where(line =>
                line.Contains("'Reportes'", StringComparison.Ordinal)
                && (line.Contains("ensure_permission(", StringComparison.Ordinal)
                    || line.Contains("sync_permission(", StringComparison.Ordinal)))
            .Select(line => line.Trim())
            .ToArray();

        Assert.Equal(2, reportesAssignments.Length);
        Assert.Contains(reportesAssignments, line => line.Contains(AdminReportes, StringComparison.Ordinal));
        Assert.Contains(
            reportesAssignments,
            line => line.Contains(RecepcionistaReportes, StringComparison.Ordinal));

        Assert.DoesNotContain("'44444444-4444-4444-4444-444444444444', 'Reportes'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("'66666666-6666-6666-6666-666666666666', 'Reportes'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("'77777777-7777-7777-7777-777777777777', 'Reportes'", sql, StringComparison.Ordinal);

        Assert.DoesNotContain(
            "'55555555-5555-5555-5555-555555555555', 'Reportes', 1, 1",
            sql,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "'11111111-1111-1111-1111-111111111111', 'Reportes', 1, 1",
            sql,
            StringComparison.Ordinal);
    }

    private static string FindExtraSeedPath(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", "extra", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"No se encontro database/seeds/extra/{fileName}");
    }

    private static string FindRootSeedPath(string fileName)
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
