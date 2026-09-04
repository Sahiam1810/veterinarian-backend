// Suite Etapa 2 (identidad de cliente vía teléfono) — tarea 2.5.
// Matriz de aceptación (contratos reales al 2026-09-04; ver también
// docs/adr/2026-09-04-client-identity-and-otp-boundaries.md).
//
// A Create con phone normalizado -> CUBIERTA.
//   Application.Tests.Clients.CreateClientCommandHandlerTests
//     .Handle_persists_the_client_with_a_normalized_phone_number
// B Phone duplicado al crear -> BLOQUEADA (sin ExistsByPhoneAsync todavía).
// C Búsqueda by-phone anónima dedicada -> BLOQUEADA (no hay endpoint by-phone).
// D Lookup staff enriquecido (tarea 2.3) -> CUBIERTA.
//   GET /api/clients/lookup + GetClientLookupQuery + Api.Tests.Clients.ClientStaffLookupHttpTests
//
// Run:
//   dotnet test --filter FullyQualifiedName~ClientsStage2AcceptanceTests
//   dotnet test --filter "FullyQualifiedName~GetClientLookup|FullyQualifiedName~ClientStaffLookup"

using Application.Clients.Abstraction;
using Xunit;

namespace Application.Tests.Clients;

public sealed class ClientsStage2AcceptanceTests
{
    private const string BlockedReason =
        "Bloqueada temporalmente por dependencia de implementación pendiente de 2.x " +
        "(ver docs/adr/2026-09-04-client-identity-and-otp-boundaries.md). " +
        "No existe contrato real (endpoint/repositorio) que ejercitar todavía.";

    // Caso B
    [Fact(Skip = BlockedReason)]
    public void PhoneDuplicado_al_crear_cliente_debe_ser_rechazado()
    {
        // Pendiente: ExistsByPhoneAsync / unicidad de PhoneNumber.
    }

    // Caso C
    [Fact(Skip = BlockedReason)]
    public void Busqueda_by_phone_debe_localizar_el_cliente_esperado()
    {
        // Pendiente: endpoint anónimo by-phone (distinto del lookup staff).
    }

    // Caso D — tarea 2.3 ya implementada (lookup staff, no entidad "Staff").
    [Fact]
    public void Lookup_staff_expone_GetByLookupAsync_en_IClientRepository()
    {
        var method = typeof(IClientRepository).GetMethod(nameof(IClientRepository.GetByLookupAsync));
        Assert.NotNull(method);
        Assert.Equal(3, method!.GetParameters().Length);
    }
}
