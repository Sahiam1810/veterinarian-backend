using Xunit;

namespace Api.Tests.Security;

// Ticket B1: Recepcionista necesita ver/crear/editar en "Chat" y "Escalamientos",
// y solo leer "Catálogos del Chat" — sin tocar los otros roles ni los módulos
// que MODULES ya tenía sembrados (modules_seed.sql, filas 17-20).
public sealed class ChatPermissionsSeedTests
{
    private const string RecepcionistaRoleId = "55555555-5555-5555-5555-555555555555";

    [Fact]
    public void Modules_seed_already_defines_the_three_chat_modules_idempotently()
    {
        var sql = File.ReadAllText(FindSeedPath("modules_seed.sql"));

        Assert.Contains("UPPER('Chat')", sql, StringComparison.Ordinal);
        Assert.Contains("UPPER('Escalamientos')", sql, StringComparison.Ordinal);
        Assert.Contains("UPPER('Catálogos del Chat')", sql, StringComparison.Ordinal);
        Assert.Contains("WHEN NOT MATCHED THEN", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(
        "ensure_permission('a5490daf-1d22-4f3a-baf6-019beea5056f', '55555555-5555-5555-5555-555555555555', 'Chat', 1, 1, 1, 0)")]
    [InlineData(
        "ensure_permission('fd167cd6-9779-4a74-be95-83afb8d53956', '55555555-5555-5555-5555-555555555555', 'Escalamientos', 1, 1, 1, 0)")]
    [InlineData(
        "ensure_permission('8358cf0d-84b4-46a6-a8b2-04583929c52f', '55555555-5555-5555-5555-555555555555', 'Catálogos del Chat', 1, 0, 0, 0)")]
    public void Role_permissions_seed_grants_the_expected_recepcionista_row(string expectedLine)
    {
        var sql = File.ReadAllText(FindSeedPath("role_permissions_seed.sql"));

        Assert.Contains(expectedLine, sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Chat")]
    [InlineData("Escalamientos")]
    [InlineData("Catálogos del Chat")]
    public void Each_chat_module_is_assigned_to_Recepcionista_exactly_once(string moduleName)
    {
        var sql = File.ReadAllText(FindSeedPath("role_permissions_seed.sql"));

        var assignments = sql
            .Split('\n')
            .Where(line =>
                line.Contains($"'{moduleName}'", StringComparison.Ordinal) &&
                line.Contains(RecepcionistaRoleId, StringComparison.Ordinal))
            .ToArray();

        Assert.Single(assignments);
    }

    [Theory]
    [InlineData("Chat")]
    [InlineData("Escalamientos")]
    [InlineData("Catálogos del Chat")]
    public void Chat_modules_are_not_granted_to_Auxiliar_or_Cliente(string moduleName)
    {
        var sql = File.ReadAllText(FindSeedPath("role_permissions_seed.sql"));

        // Auxiliar y Cliente no participan de la bandeja de escalamiento en esta ronda
        // (solo Recepcionista, decisión ya tomada). Administrador/Veterinario no se
        // excluyen aquí a propósito: el SuperAdmin se salta la matriz por completo
        // (PermissionAuthorizationHandler) y no hace falta sembrarle filas propias.
        Assert.DoesNotContain(
            $"'66666666-6666-6666-6666-666666666666', '{moduleName}'",
            sql,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            $"'77777777-7777-7777-7777-777777777777', '{moduleName}'",
            sql,
            StringComparison.Ordinal);
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
}
