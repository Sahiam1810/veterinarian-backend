using Xunit;

namespace Api.Tests.Diagnostics;

// S11: el seed de diagnósticos debe ser idempotente (MERGE por CODE) y estar cableado al setup.
public sealed class DiagnosticsSeedTests
{
    private static readonly string[] ExpectedCodes =
    [
        "PREV",
        "GASTRO",
        "DERM",
        "OTITIS",
        "INFURI",
        "RESP",
        "PARAS",
        "TRAUMA",
        "ODONT",
        "OBES",
        "CONJ",
        "OTRO",
    ];

    [Fact]
    public void Diagnostics_seed_is_idempotent_by_code_and_includes_preventive()
    {
        var sql = File.ReadAllText(FindSeedPath("diagnostics_seed.sql"));

        Assert.Contains("MERGE INTO DIAGNOSTICS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPPER(target.CODE) = UPPER(source.CODE)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHEN MATCHED THEN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHEN NOT MATCHED THEN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'PREV'", sql, StringComparison.Ordinal);
        Assert.Contains("Control preventivo / Vacunación de rutina", sql, StringComparison.Ordinal);

        foreach (var code in ExpectedCodes)
        {
            Assert.Contains($"'{code}'", sql, StringComparison.Ordinal);
        }

        // Un solo MERGE: re-ejecutar el script actualiza por CODE y no inserta duplicados.
        Assert.Equal(
            1,
            CountOccurrences(sql, "MERGE INTO DIAGNOSTICS", StringComparison.OrdinalIgnoreCase));

        Assert.False(
            File.ReadAllBytes(FindSeedPath("diagnostics_seed.sql")).AsSpan()
                .StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    [Fact]
    public void Apply_all_and_insert_all_reference_diagnostics_seed()
    {
        var applyAll = File.ReadAllText(FindSeedPath("apply_all.sql"));
        Assert.Contains("@@diagnostics_seed.sql", applyAll, StringComparison.OrdinalIgnoreCase);

        var insertAll = File.ReadAllText(FindInsertAllSeedsPath());
        Assert.Contains("MERGE INTO DIAGNOSTICS", insertAll, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'PREV'", insertAll, StringComparison.Ordinal);
        Assert.Contains("UPPER(target.CODE) = UPPER(source.CODE)", insertAll, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            1,
            CountOccurrences(insertAll, "MERGE INTO DIAGNOSTICS", StringComparison.OrdinalIgnoreCase));
    }

    private static int CountOccurrences(string haystack, string needle, StringComparison comparison)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, comparison)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string FindSeedPath(string fileName)
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

    private static string FindInsertAllSeedsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", "insert_all_seeds.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("No se encontro database/seeds/insert_all_seeds.sql");
    }
}
