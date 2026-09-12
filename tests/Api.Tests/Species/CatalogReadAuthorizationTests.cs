using Api.Common.Security.Permissions;
using Api.Races.Controllers;
using Api.Species.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Species;

// Recuperado de RegisterMyPetHttpTests (retirado en la tarea 5.2 junto con
// PetsController.RegisterMine): confirma que los catálogos de Especies/Razas
// solo exigen autenticación (cualquier rol), no un permiso granular.
public sealed class CatalogReadAuthorizationTests
{
    [Theory]
    [InlineData(typeof(SpeciesController))]
    [InlineData(typeof(RacesController))]
    public void GetAll_requires_authentication_but_no_granular_permission(Type controllerType)
    {
        var getAll = controllerType.GetMethod("GetAll")!;

        Assert.Single(getAll.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.Empty(getAll.GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true));
    }
}
