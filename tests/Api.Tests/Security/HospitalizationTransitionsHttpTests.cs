using System.Net;
using System.Text;
using System.Text.Json;
using Api.Tests.Support;
using Application.Common.Exceptions;
using Application.HospitalizationStays.Errors;
using Application.HospitalizationStays.UseCases;
using Application.MedicationOrders.UseCases;
using Application.ProcedureOrders.UseCases;
using Application.SupplyConsumptions.UseCases;
using Domain.MedicationOrders.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.SupplyConsumptions.Entities;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Api.Tests.Security;

// Contrato HTTP de las reglas de estado de Hospitalización: cada rechazo del caso de uso
// debe salir con su código (400/403/404/409) y nunca como 500.
[Collection(EnvironmentVariablesCollection.Name)]
public sealed class HospitalizationTransitionsHttpTests : IClassFixture<ModulePermissionsApiFactory>
{
    private static readonly Guid Id = Guid.NewGuid();
    private readonly ModulePermissionsApiFactory factory;

    public HospitalizationTransitionsHttpTests(ModulePermissionsApiFactory factory)
    {
        this.factory = factory;
        // Arranca el host antes del primer caso: su primera petición autenticada puede
        // cruzarse con otras clases que cambian las variables Jwt__* en paralelo.
        using var warmUp = factory.CreateClientWithPermissions("Hospitalización:View");
        warmUp.GetAsync("/api/hospitalization-stays/active").GetAwaiter().GetResult().Dispose();
    }

    private static object OrderFromStayBody() => new
    {
        ClientPetId = Guid.NewGuid(),
        AppointmentId = (Guid?)null,
        HospitalizationStayId = Id,
        IsInHouse = true,
        ReferredTo = (string?)null,
        ReferralReason = (string?)null,
        Items = new[] { new { MedicationId = Guid.NewGuid(), ProcedureId = Guid.NewGuid(), Notes = "Cada 8 horas" } }
    };

    public static IEnumerable<object[]> StayOrderRejections =>
    [
        [new ConflictException("No se pueden crear órdenes en una estancia dada de alta."), HttpStatusCode.Conflict],
        [new ForbiddenException("Solo un veterinario puede crear órdenes de hospitalización."), HttpStatusCode.Forbidden],
        [new NotFoundException("No se encontró la estancia de hospitalización."), HttpStatusCode.NotFound],
        [new BadRequestException("La mascota de la orden no coincide con la de la estancia."), HttpStatusCode.BadRequest]
    ];

    [Theory]
    [MemberData(nameof(StayOrderRejections))]
    public async Task Create_medication_order_in_stay_maps_rejection_to_status(Exception rejection, HttpStatusCode expected)
    {
        factory.Sender.Send(Arg.Any<CreateMedicationOrderCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(rejection);

        using var response = await SendAsync("POST", "/api/medication-orders", OrderFromStayBody(), "Órdenes Médicas:Create");

        await AssertRejectedAsync(response, expected, rejection.Message);
    }

    [Theory]
    [MemberData(nameof(StayOrderRejections))]
    public async Task Create_procedure_order_in_stay_maps_rejection_to_status(Exception rejection, HttpStatusCode expected)
    {
        factory.Sender.Send(Arg.Any<CreateProcedureOrderCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(rejection);

        using var response = await SendAsync("POST", "/api/procedure-orders", OrderFromStayBody(), "Órdenes Médicas:Create");

        await AssertRejectedAsync(response, expected, rejection.Message);
    }

    [Fact]
    public async Task Create_order_from_stay_sends_the_stay_and_the_authenticated_user_to_the_use_case()
    {
        CreateMedicationOrderCommand? sent = null;
        factory.Sender.Send(Arg.Do<CreateMedicationOrderCommand>(c => sent = c), Arg.Any<CancellationToken>())
            .Returns(new MedicationOrder(Guid.NewGuid(), Guid.NewGuid(), null, true, null, null,
                [(Guid.NewGuid(), (string?)null)], hospitalizationStayId: Id));

        using var response = await SendAsync("POST", "/api/medication-orders", OrderFromStayBody(), "Órdenes Médicas:Create");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(sent);
        Assert.Equal(Id, sent!.HospitalizationStayId);
        Assert.Null(sent.AppointmentId);
        Assert.NotEqual(Guid.Empty, sent.ActorUserId);
    }

    [Fact]
    public async Task Deliver_already_delivered_medication_returns_409()
    {
        const string message = "La orden de medicamento ya se encuentra entregada.";
        factory.Sender.Send(Arg.Any<CompleteMedicationOrderCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException(message));

        using var response = await SendAsync("PATCH", $"/api/medication-orders/{Id}/complete", null, "Órdenes Médicas:Edit");

        await AssertRejectedAsync(response, HttpStatusCode.Conflict, message);
    }

    [Fact]
    public async Task Complete_cancelled_procedure_returns_409()
    {
        const string message = "La orden de procedimiento está cancelada y no se puede modificar.";
        factory.Sender.Send(Arg.Any<CompleteProcedureOrderCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException(message));

        using var response = await SendAsync(
            "PATCH", $"/api/procedure-orders/{Id}/complete", new { ResultFileUrl = "https://r" }, "Órdenes Médicas:Edit");

        await AssertRejectedAsync(response, HttpStatusCode.Conflict, message);
    }

    [Fact]
    public async Task Register_consumption_after_discharge_returns_409()
    {
        const string message = "No se pueden registrar consumos en una estancia dada de alta.";
        factory.Sender.Send(Arg.Any<RegisterSupplyConsumptionCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException(message));

        using var response = await SendAsync(
            "POST", $"/api/hospitalization-stays/{Id}/supply-consumptions",
            new { SupplyId = Guid.NewGuid(), Quantity = 1 }, "Insumos:Create");

        await AssertRejectedAsync(response, HttpStatusCode.Conflict, message);

        // Restaura el comportamiento por defecto del fixture compartido.
        factory.Sender.Send(Arg.Any<RegisterSupplyConsumptionCommand>(), Arg.Any<CancellationToken>())
            .Returns((SupplyConsumption)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(SupplyConsumption)));
    }

    [Fact]
    public async Task Second_active_admission_returns_409_with_stable_code()
    {
        factory.Sender.Send(Arg.Any<AdmitHospitalizationStayCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException(
                HospitalizationStayErrorCodes.ActiveStayAlreadyExistsMessage,
                HospitalizationStayErrorCodes.ActiveStayAlreadyExists));

        using var response = await SendAsync(
            "POST", "/api/hospitalization-stays",
            new { ClientPetId = Id, AppointmentId = (Guid?)null, Motivo = "Ingreso" }, "Hospitalización:Create");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(HospitalizationStayErrorCodes.ActiveStayAlreadyExists, body);
    }

    [Fact]
    public async Task Discharge_already_discharged_stay_returns_409()
    {
        factory.Sender.Send(Arg.Any<DischargeHospitalizationStayCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("La estancia ya está dada de alta."));

        using var response = await SendAsync(
            "PATCH", $"/api/hospitalization-stays/{Id}/discharge", null, "Hospitalización:Edit");

        await AssertRejectedAsync(response, HttpStatusCode.Conflict, "La estancia ya está dada de alta.");
    }

    private async Task<HttpResponseMessage> SendAsync(string method, string route, object? body, string permission)
    {
        using var client = factory.CreateClientWithPermissions(permission);
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request);
    }

    private static async Task AssertRejectedAsync(HttpResponseMessage response, HttpStatusCode expected, string message)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(expected == response.StatusCode,
            $"Esperado {expected}, llegó {response.StatusCode}. WWW-Authenticate: {response.Headers.WwwAuthenticate}. Body: {body}");
        Assert.Contains(message, body);
    }
}
