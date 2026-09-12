using Domain.Roles;
using Xunit;

namespace Application.Tests.Roles;

public sealed class SystemRolesTests
{
    [Fact]
    public void IsSuperAdmin_accepts_only_the_canonical_identifier()
    {
        Assert.True(SystemRoles.IsSuperAdmin(SystemRoles.SuperAdminId));
        Assert.False(SystemRoles.IsSuperAdmin(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData(" superadmin ")]
    [InlineData("SUPERADMIN")]
    public void IsReservedName_accepts_trimmed_case_insensitive_name(string name)
    {
        Assert.True(SystemRoles.IsReservedName(name));
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Super Admin")]
    [InlineData("")]
    public void IsReservedName_rejects_other_names(string name)
    {
        Assert.False(SystemRoles.IsReservedName(name));
    }
}
