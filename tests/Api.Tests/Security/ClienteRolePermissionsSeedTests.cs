using System.Text.RegularExpressions;
using Xunit;

namespace Api.Tests.Security;

// T10: el rol Cliente ya no se siembra en seeds extra; no debe aparecer el GUID ni DELETE residuales.
public sealed class ClienteRolePermissionsSeedTests
{
    private const string ClienteRoleId = "77777777-7777-7777-7777-777777777777";
    private const string ClienteRoleName = "Cliente";

    [Fact]
    public void Role_permissions_seed_does_not_reference_Cliente_role_id()
    {
        var seedPath = FindExtraSeedPath("role_permissions_seed.sql");
        var sql = File.ReadAllText(seedPath);

        Assert.DoesNotContain(ClienteRoleId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM ROLE_PERMISSIONS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_seeds_use_utf8_without_bom_and_share_the_same_module_name()
    {
        var permissionPath = FindExtraSeedPath("role_permissions_seed.sql");
        var seedDirectory = Path.GetDirectoryName(permissionPath)!;
        var modulePath = Path.Combine(seedDirectory, "modules_seed.sql");
        var verificationPath = FindSeedsRootPath("verify_seeds.sql");
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

    // La regla de negocio es "ningún seed vuelve a crear el rol Cliente", no "el GUID no
    // aparece en ningún archivo": un script de auditoría necesita citarlo para verificar
    // su ausencia. Por eso se revisan solo los MERGE/INSERT reales contra ROLES.
    [Theory]
    [InlineData("extra/roles_seed.sql")]
    [InlineData("insert_all_seeds.sql")]
    public void Seeds_never_recreate_the_Cliente_role(string relativePath)
    {
        var path = FindSeedsRootPath(relativePath);
        var sql = File.ReadAllText(path);

        Assert.False(
            InsertsRole(sql, ClienteRoleId, ClienteRoleName),
            $"{Path.GetFileName(path)} vuelve a crear el rol Cliente (T10 lo retiró del modelo).");
    }

    // verify_seeds.sql es un script de auditoría: solo debe leer. Que se mantenga de solo
    // lectura es lo que hace seguro que cite el GUID/nombre del rol Cliente para comprobar
    // su ausencia, sin poder reintroducirlo por accidente.
    [Fact]
    public void Verify_seeds_script_never_writes_data()
    {
        var path = FindSeedsRootPath("verify_seeds.sql");
        var sql = File.ReadAllText(path);

        var writeStatement = Regex.Match(
            sql,
            @"\b(INSERT\s+INTO|MERGE\s+INTO|UPDATE\s+\w|DELETE\s+FROM)\b",
            RegexOptions.IgnoreCase);

        Assert.False(
            writeStatement.Success,
            $"verify_seeds.sql debe ser de solo lectura; encontrado '{writeStatement.Value}'.");
        Assert.Contains(ClienteRoleId, sql, StringComparison.OrdinalIgnoreCase);
    }

    // Busca bloques "MERGE INTO ROLES ... ;" o "INSERT INTO ROLES ... ;" y comprueba si
    // alguno inserta el id o el nombre del rol dado.
    private static bool InsertsRole(string sql, string roleId, string roleName)
    {
        foreach (Match block in Regex.Matches(
            sql,
            @"(?:MERGE\s+INTO|INSERT\s+INTO)\s+ROLES\b.*?;",
            RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            if (block.Value.Contains(roleId, StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(block.Value, $@"'{Regex.Escape(roleName)}'", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    private static string FindSeedsRootPath(string relativeFileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "seeds", relativeFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"No se encontro database/seeds/{relativeFileName}");
    }
}
