using Application.Security;
using Domain.Roles;
using Xunit;

namespace Application.Tests.Security;

public sealed class WebPlatformAccessTests
{
    [Fact]
    public void IsAllowedRoleName_accepts_the_persisted_SuperAdmin_role()
    {
        Assert.True(WebPlatformAccess.IsAllowedRoleName(SystemRoles.SuperAdminName));
    }
}
