using System.Reflection;
using Api.Common.Security.Permissions;
using Api.Reports.Controllers;
using Xunit;

namespace Api.Tests.Reports;

public sealed class ReportsAuthorizationTests
{
    [Fact]
    public void GetAppointmentsByStatus_requires_Reportes_View_permission()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetAppointmentsByStatus));
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:Reportes:{PermissionAction.View}", attr.Policy);
    }
}
