using System.Reflection;
using System.Security.Claims;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Vaccinations.Controllers;
using Application.Vaccinations.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Vaccinations;

public sealed class VaccinationsAuthorizationTests
{
    [Fact]
    public void Mine_requires_client_policy_and_clinical_history_view_permission()
    {
        var method = typeof(VaccinationsController).GetMethod(nameof(VaccinationsController.GetMine));

        Assert.NotNull(method);
        AssertPolicy(method, AuthorizationPolicies.ClientOnly);
        AssertPolicy(method, $"perm:Historiales Clínicos:{PermissionAction.View}");
    }

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

    [Fact]
    public async Task Mine_returns_unauthorized_when_authenticated_subject_is_missing()
    {
        var sender = Substitute.For<ISender>();
        var controller = new VaccinationsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"))
                }
            }
        };

        var response = await controller.GetMine(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
        await sender.DidNotReceive().Send(
            Arg.Any<GetMyVaccinationsQuery>(),
            Arg.Any<CancellationToken>());
    }

    private static void AssertPolicy(MethodInfo method, string expectedPolicy)
    {
        var policies = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.Policy);
        Assert.Contains(expectedPolicy, policies);
    }
}
