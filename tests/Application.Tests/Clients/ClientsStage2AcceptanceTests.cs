// Suite Etapa 2 (identidad de cliente vía teléfono) — tarea 2.5 / puerta de salida.
// Ver docs/smoke/etapa-2-phone-key-exit-gate.md
//
// A Create con phone normalizado -> CUBIERTA
//   CreateClientCommandHandlerTests.Handle_persists_the_client_with_a_normalized_phone_number
// B Phone duplicado al crear -> CUBIERTA
//   CreateClientCommandHandlerTests.Handle_throws_conflict_when_phone_is_already_in_use
// C by-phone anónimo -> CUBIERTA (tarea 2.2)
//   GetClientByPhoneQueryHandlerTests + ClientPhoneLookupHttpTests
// D Lookup staff -> CUBIERTA (tarea 2.3)
//   GetClientLookup* + ClientStaffLookupHttpTests
//
// Smoke puerta de salida:
//   dotnet test --filter "FullyQualifiedName~ClientsStage2AcceptanceTests|FullyQualifiedName~CreateClientCommandHandlerTests|FullyQualifiedName~GetClientByPhone|FullyQualifiedName~ClientPhoneLookup|FullyQualifiedName~GetClientLookup|FullyQualifiedName~ClientStaffLookup"

using Application.Clients.Abstraction;
using Xunit;

namespace Application.Tests.Clients;

public sealed class ClientsStage2AcceptanceTests
{
    // Caso A — ancla de matriz: el contrato vive en CreateClientCommandHandlerTests.
    [Fact]
    public void Create_con_phone_normalizado_esta_cubierto_por_handler_tests()
    {
        Assert.Contains(
            typeof(CreateClientCommandHandlerTests).GetMethods(),
            m => m.Name == nameof(CreateClientCommandHandlerTests.Handle_persists_the_client_with_a_normalized_phone_number));
    }

    // Caso B
    [Fact]
    public void Phone_duplicado_expone_ExistsByPhoneAsync_en_IClientRepository()
    {
        var method = typeof(IClientRepository).GetMethod(nameof(IClientRepository.ExistsByPhoneAsync));
        Assert.NotNull(method);
        Assert.Contains(
            typeof(CreateClientCommandHandlerTests).GetMethods(),
            m => m.Name == nameof(CreateClientCommandHandlerTests.Handle_throws_conflict_when_phone_is_already_in_use));
    }

    // Caso C
    [Fact]
    public void By_phone_expone_GetByPhoneAsync_en_IClientRepository()
    {
        var method = typeof(IClientRepository).GetMethod(nameof(IClientRepository.GetByPhoneAsync));
        Assert.NotNull(method);
        Assert.Equal(2, method!.GetParameters().Length);
    }

    // Caso D — lookup staff (cliente para staff), no entidad "Staff".
    [Fact]
    public void Lookup_staff_expone_GetByLookupAsync_en_IClientRepository()
    {
        var method = typeof(IClientRepository).GetMethod(nameof(IClientRepository.GetByLookupAsync));
        Assert.NotNull(method);
        Assert.Equal(3, method!.GetParameters().Length);
    }
}
