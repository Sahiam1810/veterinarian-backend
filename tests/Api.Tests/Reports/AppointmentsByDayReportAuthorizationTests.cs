using System.Reflection;
using Api.Common.Security.Permissions;
using Api.Reports.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Reports;

// Mismo requisito de permiso que el resto de lecturas de Citas (p. ej. AppointmentsController.GetMe):
// no existe un módulo de permisos propio de Reports, así que se reutiliza "Citas" + View.
public sealed class AppointmentsByDayReportAuthorizationTests
{
    [Fact]
    public void GetAppointmentsByDay_requires_Citas_View_permission()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetAppointmentsByDay));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal($"perm:Citas:{PermissionAction.View}", permission.Policy);

        Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }
}
