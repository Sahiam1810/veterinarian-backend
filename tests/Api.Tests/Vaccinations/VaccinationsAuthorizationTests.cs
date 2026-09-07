using System.Reflection;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Vaccinations.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Vaccinations;

public sealed class VaccinationsAuthorizationTests
{
    [Theory]
    [InlineData(nameof(VaccinationsController.GetAll))]
    [InlineData(nameof(VaccinationsController.GetById))]
    public void General_reads_require_clinical_staff_and_view_permission(string methodName)
    {
        var method = typeof(VaccinationsController).GetMethod(methodName);

        Assert.NotNull(method);
        AssertPolicy(method, AuthorizationPolicies.ClinicalStaffOnly);
        AssertPolicy(method, $"perm:Historiales Clínicos:{PermissionAction.View}");
    }

    // Tarea 5.2: cerró la superficie JWT ClientOnly del portal dueño.
    [Fact]
    public void Mine_no_longer_exists_on_the_controller()
    {
        var method = typeof(VaccinationsController).GetMethod("GetMine");

        Assert.Null(method);
    }

    private static void AssertPolicy(MethodInfo method, string expectedPolicy)
    {
        var policies = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.Policy);
        Assert.Contains(expectedPolicy, policies);
    }
}
