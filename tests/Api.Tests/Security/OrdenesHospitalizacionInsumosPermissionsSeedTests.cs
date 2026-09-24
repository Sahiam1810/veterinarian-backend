using System.Text.RegularExpressions;
using Xunit;

namespace Api.Tests.Security;

// Matriz de permisos de Órdenes Médicas, Hospitalización e Insumos. Debe ser idéntica
// en los tres scripts que siembran ROLE_PERMISSIONS:
//   extra/role_permissions_seed.sql          -> ensure_permission (solo inserta lo que falta)
//   insert_all_seeds.sql                     -> sync_permission
//   extra/role_permissions_repair_2026-09-10.sql -> sync_permission (actualiza filas existentes)
public sealed partial class OrdenesHospitalizacionInsumosPermissionsSeedTests
{
    private const string Admin = "11111111-1111-1111-1111-111111111111";
    private const string Veterinario = "44444444-4444-4444-4444-444444444444";
    private const string Recepcionista = "55555555-5555-5555-5555-555555555555";
    private const string Auxiliar = "66666666-6666-6666-6666-666666666666";

    private static readonly string[] TargetModules = ["Órdenes Médicas", "Hospitalización", "Insumos"];

    private sealed record Row(string Id, string Role, string Module, int View, int Create, int Edit, int Delete);

    private static readonly Row[] ExpectedRows =
    [
        new("7f81bb4a-a2b8-4f4e-b0d0-9fef7b20a496", Admin, "Órdenes Médicas", 1, 1, 1, 1),
        new("8862ffc5-954e-4669-b392-3ea1ac6f930a", Admin, "Hospitalización", 1, 1, 1, 1),
        new("5debd306-2148-444a-b466-85fafc2ed7d5", Admin, "Insumos", 1, 1, 1, 1),

        new("fa042789-22d8-4728-8849-0c79bc8bc916", Veterinario, "Órdenes Médicas", 1, 1, 1, 0),
        new("28a49c8b-85be-4f6a-9bd6-b3d7b8bbc6f5", Veterinario, "Hospitalización", 1, 1, 1, 0),
        new("a6f22e98-0569-4cd2-8541-26d3aa0edc77", Veterinario, "Insumos", 1, 1, 0, 0),

        new("073c9749-8dfa-450a-a84a-2ff04606e274", Recepcionista, "Órdenes Médicas", 1, 0, 1, 0),

        new("10cb7b51-5256-4faf-883e-ab27dc0e6c1a", Auxiliar, "Órdenes Médicas", 1, 0, 1, 0),
        new("fa083b49-4777-4d4f-a46c-b61d821478dd", Auxiliar, "Hospitalización", 1, 1, 0, 0),
        new("d476ccb7-90f4-4d68-a63e-f38dc6f419a7", Auxiliar, "Insumos", 1, 1, 0, 0)
    ];

    private static readonly string[] Files =
    [
        "extra/role_permissions_seed.sql",
        "insert_all_seeds.sql",
        "extra/role_permissions_repair_2026-09-10.sql"
    ];

    public static IEnumerable<object[]> SeedFiles => Files.Select(file => new object[] { file });

    [GeneratedRegex(
        @"(?:ensure|sync)_permission\('(?<id>[^']+)',\s*'(?<role>[^']+)',\s*'(?<module>[^']+)',\s*(?<v>\d),\s*(?<c>\d),\s*(?<e>\d),\s*(?<d>\d)\)")]
    private static partial Regex PermissionCall();

    [Theory]
    [MemberData(nameof(SeedFiles))]
    public void Each_seed_file_matches_the_expected_matrix_exactly(string relativePath)
    {
        var rows = ReadTargetRows(relativePath);

        Assert.Equal(ExpectedRows.Length, rows.Count);
        foreach (var expected in ExpectedRows)
        {
            Assert.Contains(expected, rows);
        }
    }

    [Fact]
    public void The_three_seed_files_carry_the_same_matrix()
    {
        var reference = ReadTargetRows(Files[0]).OrderBy(r => r.Id).ToArray();

        foreach (var path in Files.Skip(1))
        {
            var other = ReadTargetRows(path).OrderBy(r => r.Id).ToArray();
            Assert.Equal(reference, other);
        }
    }

    [Theory]
    [MemberData(nameof(SeedFiles))]
    public void Only_Administrador_can_delete_and_Recepcionista_has_no_Hospitalizacion_or_Insumos(string relativePath)
    {
        var rows = ReadTargetRows(relativePath);

        Assert.All(rows.Where(r => r.Role != Admin), r => Assert.Equal(0, r.Delete));
        Assert.DoesNotContain(rows, r => r.Role == Recepcionista && r.Module != "Órdenes Médicas");
        Assert.DoesNotContain(rows, r => r.Role is Veterinario or Auxiliar && r.Delete == 1);
    }

    [Theory]
    [MemberData(nameof(SeedFiles))]
    public void Permission_ids_are_unique_within_each_seed_file(string relativePath)
    {
        var sql = File.ReadAllText(FindSeedPath(relativePath));
        var ids = PermissionCall().Matches(sql).Select(m => m.Groups["id"].Value.ToLowerInvariant()).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        foreach (var expected in ExpectedRows)
        {
            Assert.Single(ids, id => id == expected.Id);
        }
    }

    [Fact]
    public void Repair_script_warns_that_it_overwrites_existing_rows()
    {
        var sql = File.ReadAllText(FindSeedPath("extra/role_permissions_repair_2026-09-10.sql"));

        Assert.Contains("ADVERTENCIA", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Modules_are_defined_before_the_permissions_reference_them()
    {
        var modulesSeed = File.ReadAllText(FindSeedPath("extra/modules_seed.sql"));
        var insertAll = File.ReadAllText(FindSeedPath("insert_all_seeds.sql"));

        foreach (var module in TargetModules)
        {
            Assert.Contains($"'{module}'", modulesSeed, StringComparison.Ordinal);
            Assert.Contains($"'{module}' AS NAME", insertAll, StringComparison.Ordinal);
        }
    }

    private static List<Row> ReadTargetRows(string relativePath)
    {
        var sql = File.ReadAllText(FindSeedPath(relativePath));

        return PermissionCall().Matches(sql)
            .Select(m => new Row(
                m.Groups["id"].Value.ToLowerInvariant(),
                m.Groups["role"].Value,
                m.Groups["module"].Value,
                int.Parse(m.Groups["v"].Value),
                int.Parse(m.Groups["c"].Value),
                int.Parse(m.Groups["e"].Value),
                int.Parse(m.Groups["d"].Value)))
            .Where(r => TargetModules.Contains(r.Module))
            .ToList();
    }

    private static string FindSeedPath(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"No se encontro database/seeds/{relativePath}");
    }
}
