using Application.UserAccounts.UseCase;
using Xunit;

namespace Application.Tests.UserAccounts;

// P2 corregido: Status era texto libre (solo NotEmpty + MaxLength), y
// AuthenticationService.IsActiveAccount compara con Ordinal exacto contra
// "Activo" -- un typo ("activo", "Active") dejaba una cuenta bloqueada sin
// ningún error de validación que lo avisara.
public sealed class UserAccountStatusValidationTests
{
    private readonly CreateUserAccountCommandValidator createValidator = new();
    private readonly UpdateUserAccountCommandValidator updateValidator = new();

    [Theory]
    [InlineData("Activo")]
    [InlineData("Inactivo")]
    public async Task Create_accepts_allowed_status_values(string status)
    {
        var result = await createValidator.ValidateAsync(ValidCreate() with { Status = status });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("activo")]
    [InlineData("Active")]
    [InlineData("ACTIVO")]
    [InlineData("Suspendido")]
    public async Task Create_rejects_status_values_outside_the_allowed_list(string status)
    {
        var result = await createValidator.ValidateAsync(ValidCreate() with { Status = status });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateUserAccountCommand.Status));
    }

    [Theory]
    [InlineData("Activo")]
    [InlineData("Inactivo")]
    public async Task Update_accepts_allowed_status_values(string status)
    {
        var result = await updateValidator.ValidateAsync(ValidUpdate() with { Status = status });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("activo")]
    [InlineData("Active")]
    public async Task Update_rejects_status_values_outside_the_allowed_list(string status)
    {
        var result = await updateValidator.ValidateAsync(ValidUpdate() with { Status = status });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(UpdateUserAccountCommand.Status));
    }

    private static CreateUserAccountCommand ValidCreate() => new(
        UserId: Guid.NewGuid(),
        Username: "ana",
        Mail: "ana@huellitas.test",
        Status: "Activo");

    private static UpdateUserAccountCommand ValidUpdate() => new(
        Id: Guid.NewGuid(),
        Username: "ana",
        Mail: "ana@huellitas.test",
        Status: "Activo");
}
