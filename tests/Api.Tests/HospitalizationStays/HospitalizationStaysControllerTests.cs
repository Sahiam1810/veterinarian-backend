using System.Net;
using System.Reflection;
using Api.Common.Security.Permissions;
using Api.HospitalizationStays.Controllers;
using Api.Tests.Security;
using Xunit;

namespace Api.Tests.HospitalizationStays;

public sealed class HospitalizationStaysControllerAttributeTests
{
    [Theory]
    [InlineData(nameof(HospitalizationStaysController.GetActive), PermissionAction.View)]
    [InlineData(nameof(HospitalizationStaysController.GetById), PermissionAction.View)]
    [InlineData(nameof(HospitalizationStaysController.GetByPet), PermissionAction.View)]
    [InlineData(nameof(HospitalizationStaysController.GetNotes), PermissionAction.View)]
    [InlineData(nameof(HospitalizationStaysController.GetStaff), PermissionAction.View)]
    [InlineData(nameof(HospitalizationStaysController.Admit), PermissionAction.Create)]
    [InlineData(nameof(HospitalizationStaysController.AddNote), PermissionAction.Create)]
    [InlineData(nameof(HospitalizationStaysController.Discharge), PermissionAction.Edit)]
    [InlineData(nameof(HospitalizationStaysController.RegisterPayment), PermissionAction.Edit)]
    public void Endpoint_has_correct_RequirePermission_attribute(string methodName, PermissionAction expectedAction)
    {
        var method = typeof(HospitalizationStaysController).GetMethod(methodName);
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:Hospitalización:{expectedAction}", attr.Policy);
    }
}

[Collection("AuthSecurityHttp")]
public sealed class HospitalizationStaysControllerHttpTests
{
    private readonly AuthSecurityHttpApiFactory factory;

    public HospitalizationStaysControllerHttpTests(AuthSecurityHttpApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetActive_and_GetStaff_return_403_without_permission()
    {
        using var client = factory.CreateAnonymousClient();
        var tokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffAPassword);

        AuthSecurityHttpTestHelpers.WithBearer(client, tokens.AccessToken);

        using var activeResponse = await client.GetAsync("/api/hospitalization-stays/active");
        using var staffResponse = await client.GetAsync("/api/hospitalization-stays/staff");

        Assert.Equal(HttpStatusCode.Forbidden, activeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, staffResponse.StatusCode);
    }

    [Fact]
    public async Task GetActive_and_GetStaff_return_200_with_permission()
    {
        using var client = factory.CreateAnonymousClient();

        // SuperAdmin token has all permissions in AuthSecurityHttpApiFactory
        var tokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.SuperAdminEmail,
            AuthSecurityTestUsers.SuperAdminPassword);

        AuthSecurityHttpTestHelpers.WithBearer(client, tokens.AccessToken);

        using var activeResponse = await client.GetAsync("/api/hospitalization-stays/active");
        using var staffResponse = await client.GetAsync("/api/hospitalization-stays/staff");

        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
    }
}
