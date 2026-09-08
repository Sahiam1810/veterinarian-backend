using System.Reflection;
using Api.Common.Security.Permissions;
using Api.Reports.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Reports;

public sealed class ReportsAuthorizationTests
{
    [Fact]
    public void Appointments_by_veterinarian_requires_the_Reportes_view_permission()
    {
        var action = typeof(ReportsController).GetMethod(nameof(ReportsController.GetAppointmentsByVeterinarian));

        Assert.NotNull(action);
        var permission = action.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal("perm:Reportes:View", permission.Policy);
        Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
