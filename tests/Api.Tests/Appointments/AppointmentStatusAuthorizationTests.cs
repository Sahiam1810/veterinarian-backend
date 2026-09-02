using System.Reflection;
using Api.AppointmentStatusHistories.Controllers;
using Api.Appointments.Controllers;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class AppointmentStatusAuthorizationTests
{
    [Fact]
    public void STA_T16_AppointmentsController_UpdateStatus_requires_Citas_Edit_permission()
    {
        var method = typeof(AppointmentsController).GetMethod(nameof(AppointmentsController.UpdateStatus));
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:Citas:{PermissionAction.Edit}", attr.Policy);
    }

    [Theory]
    [InlineData(nameof(AppointmentStatusHistoriesController.Create))]
    [InlineData(nameof(AppointmentStatusHistoriesController.Update))]
    [InlineData(nameof(AppointmentStatusHistoriesController.Delete))]
    public void STA_T17_AppointmentStatusHistories_mutations_require_AdminOnly(string methodName)
    {
        var method = typeof(AppointmentStatusHistoriesController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorizeAttr.Policy);
        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }

    [Theory]
    [InlineData(nameof(AppointmentStatusHistoriesController.GetAll))]
    [InlineData(nameof(AppointmentStatusHistoriesController.GetById))]
    public void STA_T18_AppointmentStatusHistories_reads_keep_StaffOnly_policy(string methodName)
    {
        var method = typeof(AppointmentStatusHistoriesController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.StaffOnly, authorizeAttr.Policy);
    }
}
