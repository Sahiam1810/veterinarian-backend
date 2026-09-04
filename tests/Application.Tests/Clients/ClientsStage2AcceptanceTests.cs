// Suite Etapa 2 (identidad de cliente vía teléfono) — tarea 2.5.
// Matriz de aceptación (contratos reales al 2026-09-04; ver también
// docs/adr/2026-09-04-client-identity-and-otp-boundaries.md, que declara
// explícitamente que unicidad de teléfono y OTP telefónico NO están
// implementados hoy y requieren esquema y flujos explícitos).
//
// A Create con phone normalizado -> CUBIERTA. El handler real
//   (CreateClientCommandHandler) pasa el PhoneNumber crudo del request a
//   ClientEntity, que lo normaliza vía ClientPhoneNumber.CreateOptional
//   (solo dígitos). Test real:
//   Application.Tests.Clients.CreateClientCommandHandlerTests
//     .Handle_persists_the_client_with_a_normalized_phone_number
//   No se duplica aquí para no probar dos veces el mismo contrato.
// B Phone duplicado al crear -> BLOQUEADA. CreateClientCommandHandler solo
//   valida IClientRepository.ExistsByIdentificationNumberAsync y
//   ExistsByUserIdAsync; no existe ExistsByPhoneAsync ni ningún chequeo de
//   unicidad de PhoneNumber en el handler, el validador
//   (CreateClientCommandValidator) ni el repositorio (IClientRepository).
//   Pendiente de implementación 2.x -- no se agrega aquí para no inventar
//   el contrato de rechazo (código de error, excepción) que todavía no existe.
// C Búsqueda by-phone -> CUBIERTA (tarea 2.2). Contratos reales:
//   Application.Tests.Clients.GetClientByPhoneQueryHandlerTests
//   Api.Tests.Clients.ClientPhoneLookupHttpTests (200/404)
//   Api.Tests.Clients.ClientPhoneLookupRateLimitHttpTests (429)
//   No se duplica aquí para no probar dos veces el mismo contrato.
// D Lookup de staff -> BLOQUEADA. No existe un concepto de dominio "Staff"
//   en el proyecto: "StaffOnly"/"ClinicalStaffOnly"/"FrontDeskStaffOnly" son
//   nombres de políticas de autorización (AuthorizationPolicies), no una
//   entidad ni un repositorio de personal. El concepto de dominio más
//   cercano (Veterinarian / IVeterinarianRepository) no tiene lookup por
//   teléfono ni por ningún criterio genérico de "staff".
//
// Run (solo los casos bloqueados documentados en este archivo):
//   dotnet test --filter FullyQualifiedName~ClientsStage2AcceptanceTests
// Run (matriz completa de Etapa 2, incluyendo el caso A real y by-phone 2.2):
//   dotnet test --filter "FullyQualifiedName~ClientsStage2AcceptanceTests|FullyQualifiedName~CreateClientCommandHandlerTests|FullyQualifiedName~GetClientByPhoneQueryHandlerTests|FullyQualifiedName~ClientPhoneLookup"

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
        // Intencionalmente vacío: no hay ExistsByPhoneAsync ni chequeo de
        // unicidad de PhoneNumber en CreateClientCommandHandler/IClientRepository
        // contra el cual escribir una aserción real sin inventar el contrato.
    }

    // Caso D
    [Fact(Skip = BlockedReason)]
    public void Lookup_de_staff_debe_resolver_el_criterio_de_busqueda()
    {
        // Intencionalmente vacío: no existe entidad ni repositorio de "Staff"
        // en el dominio -- solo políticas de autorización con ese nombre.
    }
}
