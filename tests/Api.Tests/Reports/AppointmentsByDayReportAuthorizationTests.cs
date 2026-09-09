using System.Reflection;
using Api.Common.Security.Permissions;
using Api.Reports.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Reports;

// Módulo de permisos "Reportes" (sembrado en B1, ver database/seeds/role_permissions_seed.sql):
// solo Administrador tiene Reportes:View, a diferencia de Citas:View que tiene todo el staff.
public sealed class AppointmentsByDayReportAuthorizationTests
{
    [Fact]
    public void GetAppointmentsByDay_requires_Reportes_View_permission()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetAppointmentsByDay));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal($"perm:Reportes:{PermissionAction.View}", permission.Policy);

        Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }
}
