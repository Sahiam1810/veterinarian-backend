using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Api.Common.Security.Permissions;
using Api.HospitalizationStays.Controllers;
using Api.MedicationOrders.Controllers;
using Api.ProcedureOrders.Controllers;
using Api.Supplies.Controllers;
using Api.SupplyConsumptions.Controllers;
using Api.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Api.Tests.Security;

[Collection(EnvironmentVariablesCollection.Name)]
public sealed class ModulePermissionsHttpTests(ModulePermissionsApiFactory factory)
    : IClassFixture<ModulePermissionsApiFactory>
{
    private static readonly Guid TestGuid = Guid.NewGuid();

    // Controllers subject to strict RequirePermission inspection
    public static readonly Type[] ControllersToScan =
    [
        typeof(HospitalizationStaysController),
        typeof(SuppliesController),
        typeof(SupplyConsumptionsController),
        typeof(MedicationOrdersController),
        typeof(ProcedureOrdersController)
    ];

    [Fact]
    public void Reflection_AllControllerActions_HaveRequirePermissionOrAllowAnonymous()
    {
        var unprotectedActions = new List<string>();

        foreach (var controller in ControllersToScan)
        {
            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && (typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                            m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));

            foreach (var action in actions)
            {
                var hasRequirePermission = action.GetCustomAttribute<RequirePermissionAttribute>() != null ||
                                            controller.GetCustomAttribute<RequirePermissionAttribute>() != null;

                var hasAllowAnonymous = action.GetCustomAttribute<AllowAnonymousAttribute>() != null ||
                                          controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;

                if (!hasRequirePermission && !hasAllowAnonymous)
                {
                    unprotectedActions.Add($"{controller.Name}.{action.Name}");
                }
            }
        }

        Assert.Empty(unprotectedActions);
    }

    public static IEnumerable<object?[]> EndpointMatrix =>
    [
        // Hospitalización
        new object?[] { "POST", "/api/hospitalization-stays", "Hospitalización", "Create", "View", new { ClientPetId = TestGuid, AppointmentId = (Guid?)null, Motivo = "Prueba de ingreso" } },
        new object?[] { "PATCH", $"/api/hospitalization-stays/{TestGuid}/discharge", "Hospitalización", "Edit", "View", null },
        new object?[] { "GET", $"/api/hospitalization-stays/{TestGuid}", "Hospitalización", "View", "Create", null },
        new object?[] { "GET", $"/api/hospitalization-stays/{TestGuid}/invoice", "Hospitalización", "View", "Create", null },
        new object?[] { "GET", "/api/hospitalization-stays/active", "Hospitalización", "View", "Create", null },
        new object?[] { "GET", $"/api/hospitalization-stays/pet/{TestGuid}", "Hospitalización", "View", "Create", null },
        new object?[] { "POST", $"/api/hospitalization-stays/{TestGuid}/notes", "Hospitalización", "Create", "View", new { Nota = "Nota prueba", EntregadoAUserId = (Guid?)null } },
        new object?[] { "GET", $"/api/hospitalization-stays/{TestGuid}/notes", "Hospitalización", "View", "Create", null },
        new object?[] { "GET", "/api/hospitalization-stays/staff", "Hospitalización", "View", "Create", null },

        // Insumos
        new object?[] { "GET", "/api/supplies", "Insumos", "View", "Create", null },
        new object?[] { "GET", $"/api/supplies/{TestGuid}", "Insumos", "View", "Create", null },
        new object?[] { "POST", "/api/supplies", "Insumos", "Create", "View", new { Name = "Gasa Estéril", Unit = "unidad", UnitPrice = 1500m, Stock = 10m, IsActive = true } },
        new object?[] { "PUT", $"/api/supplies/{TestGuid}", "Insumos", "Edit", "View", new { Name = "Gasa Estéril", Unit = "unidad", UnitPrice = 1800m, Stock = 20m, IsActive = true } },
        new object?[] { "DELETE", $"/api/supplies/{TestGuid}", "Insumos", "Delete", "View", null },
        new object?[] { "POST", $"/api/hospitalization-stays/{TestGuid}/supply-consumptions", "Insumos", "Create", "View", new { SupplyId = TestGuid, Quantity = 2 } },
        new object?[] { "GET", $"/api/hospitalization-stays/{TestGuid}/supply-consumptions", "Insumos", "View", "Create", null },
        new object?[] { "GET", $"/api/hospitalization-stays/{TestGuid}/supply-consumptions/total", "Insumos", "View", "Create", null },

        // Órdenes Médicas Pendientes
        new object?[] { "GET", "/api/medication-orders/pending", "Órdenes Médicas", "View", "Create", null },
        new object?[] { "GET", "/api/procedure-orders/pending", "Órdenes Médicas", "View", "Create", null }
    ];

    [Theory]
    [MemberData(nameof(EndpointMatrix))]
    public async Task Endpoint_Without_Token_Returns_401(
        string method,
        string route,
        string module,
        string requiredAction,
        string wrongAction,
        object? body)
    {
        _ = module;
        _ = requiredAction;
        _ = wrongAction;

        using var client = factory.CreateGuestClient();
        using var request = CreateRequest(method, route, body);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(EndpointMatrix))]
    public async Task Endpoint_With_Same_Module_Wrong_Action_Returns_403(
        string method,
        string route,
        string module,
        string requiredAction,
        string wrongAction,
        object? body)
    {
        _ = requiredAction;
        var wrongPermission = $"{module}:{wrongAction}";

        using var client = factory.CreateClientWithPermissions(wrongPermission);
        using var request = CreateRequest(method, route, body);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(EndpointMatrix))]
    public async Task Endpoint_With_Other_Module_Same_Action_Returns_403(
        string method,
        string route,
        string module,
        string requiredAction,
        string wrongAction,
        object? body)
    {
        _ = module;
        _ = wrongAction;
        var otherModulePermission = $"ModuloDePruebaExtrano:{requiredAction}";

        using var client = factory.CreateClientWithPermissions(otherModulePermission);
        using var request = CreateRequest(method, route, body);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(EndpointMatrix))]
    public async Task Endpoint_With_Exact_Required_Permission_Passes_Gate(
        string method,
        string route,
        string module,
        string requiredAction,
        string wrongAction,
        object? body)
    {
        _ = wrongAction;
        var exactPermission = $"{module}:{requiredAction}";

        using var client = factory.CreateClientWithPermissions(exactPermission);
        using var request = CreateRequest(method, route, body);

        using var response = await client.SendAsync(request);

        // Con el permiso exacto la respuesta es de éxito (no solo "pasó la puerta"): así un
        // body inválido o un endpoint que revienta con 500 tampoco pasa desapercibido.
        Assert.Equal(ExpectedSuccess(method), response.StatusCode);
    }

    private static HttpStatusCode ExpectedSuccess(string httpMethod) => httpMethod switch
    {
        "GET" => HttpStatusCode.OK,
        "POST" => HttpStatusCode.Created,
        _ => HttpStatusCode.NoContent // PUT, PATCH y DELETE
    };

    private static HttpRequestMessage CreateRequest(string httpMethod, string route, object? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), route);
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        return request;
    }
}
