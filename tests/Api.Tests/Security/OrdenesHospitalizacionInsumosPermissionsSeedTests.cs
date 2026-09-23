using System.Text.RegularExpressions;
using Xunit;

namespace Api.Tests.Security;

public sealed class OrdenesHospitalizacionInsumosPermissionsSeedTests
{
    private const string AdminRoleId = "11111111-1111-1111-1111-111111111111";
    private const string VeterinarioRoleId = "44444444-4444-4444-4444-444444444444";
    private const string RecepcionistaRoleId = "55555555-5555-5555-5555-555555555555";
    private const string AuxiliarRoleId = "66666666-6666-6666-6666-666666666666";

    private static readonly string[] TargetModules =
    [
        "Órdenes Médicas",
        "Hospitalización",
        "Insumos"
    ];

    private static readonly string[] ExpectedPermissionLines =
    [
        // Administrador: (1,1,1,1) en los 3 módulos
        $"sync_permission('7f81bb4a-a2b8-4f4e-b0d0-9fef7b20a496', '{AdminRoleId}', 'Órdenes Médicas', 1, 1, 1, 1);",
        $"sync_permission('8862ffc5-954e-4669-b392-3ea1ac6f930a', '{AdminRoleId}', 'Hospitalización', 1, 1, 1, 1);",
        $"sync_permission('5debd306-2148-444a-b466-85fafc2ed7d5', '{AdminRoleId}', 'Insumos', 1, 1, 1, 1);",

        // Veterinario: Órdenes (1,1,1,0), Hospitalización (1,1,1,0), Insumos (1,1,0,0)
        $"sync_permission('fa042789-22d8-4728-8849-0c79bc8bc916', '{VeterinarioRoleId}', 'Órdenes Médicas', 1, 1, 1, 0);",
        $"sync_permission('28a49c8b-85be-4f6a-9bd6-b3d7b8bbc6f5', '{VeterinarioRoleId}', 'Hospitalización', 1, 1, 1, 0);",
        $"sync_permission('a6f22e98-0569-4cd2-8541-26d3aa0edc77', '{VeterinarioRoleId}', 'Insumos', 1, 1, 0, 0);",

        // Recepcionista: Órdenes (1,0,1,0) - Sin fila en Hospitalización ni en Insumos
        $"sync_permission('073c9749-8dfa-450a-a84a-2ff04606e274', '{RecepcionistaRoleId}', 'Órdenes Médicas', 1, 0, 1, 0);",

        // Auxiliar: Órdenes (1,0,1,0), Hospitalización (1,1,0,0), Insumos (1,1,0,0)
        $"sync_permission('10cb7b51-5256-4faf-883e-ab27dc0e6c1a', '{AuxiliarRoleId}', 'Órdenes Médicas', 1, 0, 1, 0);",
        $"sync_permission('fa083b49-4777-4d4f-a46c-b61d821478dd', '{AuxiliarRoleId}', 'Hospitalización', 1, 1, 0, 0);",
        $"sync_permission('d476ccb7-90f4-4d68-a63e-f38dc6f419a7', '{AuxiliarRoleId}', 'Insumos', 1, 1, 0, 0);"
    ];

    private static readonly string[] NewPermissionUuids =
    [
        "7f81bb4a-a2b8-4f4e-b0d0-9fef7b20a496",
        "8862ffc5-954e-4669-b392-3ea1ac6f930a",
        "5debd306-2148-444a-b466-85fafc2ed7d5",
        "fa042789-22d8-4728-8849-0c79bc8bc916",
        "28a49c8b-85be-4f6a-9bd6-b3d7b8bbc6f5",
        "a6f22e98-0569-4cd2-8541-26d3aa0edc77",
        "073c9749-8dfa-450a-a84a-2ff04606e274",
        "10cb7b51-5256-4faf-883e-ab27dc0e6c1a",
        "fa083b49-4777-4d4f-a46c-b61d821478dd",
        "d476ccb7-90f4-4d68-a63e-f38dc6f419a7"
    ];

    [Fact]
    public void Modules_section_defines_expected_modules()
    {
        var sql = File.ReadAllText(FindRootSeedPath("insert_all_seeds.sql"));

        foreach (var module in TargetModules)
        {
            Assert.Contains($"'{module}'", sql, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Insert_all_seeds_matches_expected_permission_matrix()
    {
        var sql = File.ReadAllText(FindRootSeedPath("insert_all_seeds.sql"));

        var assignments = sql
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("sync_permission(") && TargetModules.Any(m => line.Contains($"'{m}'", StringComparison.Ordinal)))
            .ToArray();

        Assert.Equal(ExpectedPermissionLines.Length, assignments.Length);

        foreach (var expectedLine in ExpectedPermissionLines)
        {
            Assert.Contains(assignments, line => line.Equals(expectedLine, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Role_restrictions_negative_cases()
    {
        var sql = File.ReadAllText(FindRootSeedPath("insert_all_seeds.sql"));

        var permissionCalls = sql
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("sync_permission("))
            .ToArray();

        // 1. Recepcionista NO debe tener filas ni permisos para Hospitalización ni Insumos
        var recepUnexpectedCalls = permissionCalls
            .Where(line => line.Contains(RecepcionistaRoleId, StringComparison.Ordinal)
                           && (line.Contains("'Hospitalización'", StringComparison.Ordinal)
                               || line.Contains("'Insumos'", StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(recepUnexpectedCalls);

        // 2. Auxiliar NO debe tener permiso de Eliminar en ninguno de estos 3 módulos
        var auxDeleteCalls = permissionCalls
            .Where(line => line.Contains(AuxiliarRoleId, StringComparison.Ordinal)
                           && TargetModules.Any(m => line.Contains($"'{m}'", StringComparison.Ordinal))
                           && Regex.IsMatch(line, @",\s*1\s*\);$"))
            .ToArray();

        Assert.Empty(auxDeleteCalls);

        // 3. Veterinario NO debe tener permiso de Eliminar en ninguno de estos 3 módulos
        var vetDeleteCalls = permissionCalls
            .Where(line => line.Contains(VeterinarioRoleId, StringComparison.Ordinal)
                           && TargetModules.Any(m => line.Contains($"'{m}'", StringComparison.Ordinal))
                           && Regex.IsMatch(line, @",\s*1\s*\);$"))
            .ToArray();

        Assert.Empty(vetDeleteCalls);
    }

    [Fact]
    public void Uuids_are_unique_and_do_not_collide()
    {
        var sql = File.ReadAllText(FindRootSeedPath("insert_all_seeds.sql"));

        // Validar que todos los UUIDs nuevos son válidos y distintos entre sí
        var parsedGuids = NewPermissionUuids.Select(Guid.Parse).ToHashSet();
        Assert.Equal(NewPermissionUuids.Length, parsedGuids.Count);

        // Validar que ninguno de los UUIDs colisiona en el archivo (aparece exactamente una vez)
        foreach (var uuid in NewPermissionUuids)
        {
            var matches = Regex.Matches(sql, Regex.Escape(uuid), RegexOptions.IgnoreCase);
            Assert.Single(matches);
        }

        // Validar adicionalmente que todos los UUIDs pasados al primer argumento de sync_permission sean únicos en el archivo
        var syncPermissionUuidMatches = Regex.Matches(sql, @"sync_permission\('([a-f0-9\-]+)'", RegexOptions.IgnoreCase);
        var syncUuids = syncPermissionUuidMatches.Select(m => m.Groups[1].Value.ToLowerInvariant()).ToList();
        var distinctSyncUuids = syncUuids.Distinct().ToList();

        Assert.Equal(syncUuids.Count, distinctSyncUuids.Count);
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
