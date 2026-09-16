using System.Security.Claims;
using Api.Auth.Controllers;
using Api.Auth.Dtos;
using Application.Modules.UseCases;
using Application.Permissions.Claims;
using Application.Permissions.UseCases;
using Domain.Roles;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;

namespace Api.Tests.Security;

// P1: GET /api/auth/permissions devolvía 401 para un SuperAdmin autenticado
// (su token no lleva role_id) en vez de reflejar que se salta toda la matriz
// de permisos, como ya hace PermissionAuthorizationHandler en el resto de la API.
public sealed class AuthControllerPermissionsTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PersonId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task Permissions_returns_all_modules_as_fully_granted_for_a_superadmin_token()
    {
        sender.Send(Arg.Any<GetAllModulesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ModuleEntity("Clientes", null),
                new ModuleEntity("Citas", null)
            });

        var controller = CreateController(
            claims: [new Claim("role_id", SystemRoles.SuperAdminId.ToString())]);

        var result = await controller.Permissions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserPermissionsResponseDto>(ok.Value);
        Assert.Equal(2, dto.Permissions.Count);
        Assert.All(dto.Permissions.Values, permission =>
        {
            Assert.True(permission.CanView);
            Assert.True(permission.CanCreate);
            Assert.True(permission.CanEdit);
            Assert.True(permission.CanDelete);
        });
        await sender.DidNotReceive().Send(Arg.Any<GetUserEffectivePermissionsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Permissions_reconstructs_the_complete_matrix_from_the_current_token()
    {
        sender.Send(Arg.Any<GetAllModulesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ModuleEntity("Clientes", null),
                new ModuleEntity("Mascotas", null)
            });

        var controller = CreateController(claims:
        [
            new Claim("role_id", RoleId.ToString()),
            new Claim("person_id", PersonId.ToString()),
            new Claim(PermissionClaimValue.ClaimType, "perm:Clientes:View"),
            new Claim(PermissionClaimValue.ClaimType, "perm:Clientes:Edit"),
            new Claim(PermissionClaimValue.ClaimType, "perm:ModuloInexistente:Delete"),
            new Claim(PermissionClaimValue.ClaimType, "valor-invalido")
        ]);

        var result = await controller.Permissions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserPermissionsResponseDto>(ok.Value);
        Assert.Equal(2, dto.Permissions.Count);
        Assert.True(dto.Permissions["Clientes"].CanView);
        Assert.False(dto.Permissions["Clientes"].CanCreate);
        Assert.True(dto.Permissions["Clientes"].CanEdit);
        Assert.False(dto.Permissions["Clientes"].CanDelete);
        Assert.False(dto.Permissions["Mascotas"].CanView);
        await sender.DidNotReceive().Send(
            Arg.Any<GetUserEffectivePermissionsQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Permissions_returns_unauthorized_when_neither_superadmin_nor_role_id_are_present()
    {
        var controller = CreateController(claims: []);

        var result = await controller.Permissions(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private AuthController CreateController(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        return new AuthController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }
}
