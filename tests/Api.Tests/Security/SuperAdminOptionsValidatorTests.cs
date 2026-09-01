using Infrastructure.Security.Options;
using Xunit;

namespace Api.Tests.Security;

public sealed class SuperAdminOptionsValidatorTests
{
    private static readonly Guid ValidId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private const string ValidEmail = "superadmin@huellitas.test";
    private const string ValidPasswordHash = "100000.c2FsdA==.a2V5";

    private readonly SuperAdminOptionsValidator validator = new();

    [Fact]
    public void Validate_succeeds_when_disabled_regardless_of_other_values()
    {
        var options = CreateOptions(
            enabled: false,
            id: Guid.Empty,
            email: "not-an-email",
            passwordHash: "not-a-hash");

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_accepts_a_well_formed_configuration()
    {
        var result = validator.Validate(null, CreateOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_rejects_empty_id_when_enabled()
    {
        var result = validator.Validate(null, CreateOptions(id: Guid.Empty));

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_rejects_invalid_email_when_enabled(string email)
    {
        var result = validator.Validate(null, CreateOptions(email: email));

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text-password")]
    [InlineData("not-numeric.salt.key")]
    public void Validate_rejects_a_password_hash_with_the_wrong_shape(string passwordHash)
    {
        var result = validator.Validate(null, CreateOptions(passwordHash: passwordHash));

        Assert.True(result.Failed);
    }

    private static SuperAdminOptions CreateOptions(
        bool enabled = true,
        Guid? id = null,
        string email = ValidEmail,
        string passwordHash = ValidPasswordHash) => new()
    {
        Enabled = enabled,
        Id = id ?? ValidId,
        Email = email,
        PasswordHash = passwordHash
    };
}
