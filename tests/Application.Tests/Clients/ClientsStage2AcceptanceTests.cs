// Suite Etapa 2 (identidad de cliente vía teléfono) — tarea 2.5 / 2.1.
// Matriz de aceptación (contratos reales al 2026-09-04).
//
// A Create con phone normalizado -> CUBIERTA.
//   Application.Tests.Clients.CreateClientCommandHandlerTests
//     .Handle_persists_the_client_with_a_normalized_phone_number
// B Phone duplicado al crear -> CUBIERTA (tarea 2.1).
//   CreateClientCommandHandlerTests.Handle_throws_typed_conflict_when_phone_already_exists
//   (+ formatos equivalentes / null-whitespace).
// C Búsqueda by-phone -> BLOQUEADA (fuera de alcance 2.1).
// D Lookup de staff -> BLOQUEADA (fuera de alcance 2.1).
//
// Run:
//   dotnet test --filter FullyQualifiedName~ClientsStage2AcceptanceTests
//   dotnet test --filter "FullyQualifiedName~ClientsStage2AcceptanceTests|FullyQualifiedName~CreateClientCommandHandlerTests|FullyQualifiedName~UpdateClientCommandHandlerTests"

using Application.Clients.Abstraction;
using Application.Clients.Errors;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Clients;

public sealed class ClientsStage2AcceptanceTests
{
    private const string BlockedReason =
        "Bloqueada: fuera del alcance de la tarea 2.1 (unicidad de teléfono en escritura). " +
        "No existe contrato real by-phone / staff lookup que ejercitar.";

    // Caso B — ahora implementado en 2.1
    [Fact]
    public async Task PhoneDuplicado_al_crear_cliente_debe_ser_rechazado()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clientsRepository = Substitute.For<IClientRepository>();
        var usersRepository = Substitute.For<IUsersRepository>();
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);

        var user = new UserEntity("Ana Cliente", "stage2@huellitas.test", "hash", Guid.NewGuid());
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(false);
        clientsRepository.ExistsByPhoneAsync("3001112233", null, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new CreateClientCommandHandler(unitOfWork);
        var command = new CreateClientCommand(
            user.Id, "9988776655", "Calle Falsa 123", PhoneNumber: "3001112233");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal(ClientErrorCodes.PhoneNumberAlreadyExists, ex.Code);
        await clientsRepository.DidNotReceive().AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
    }

    // Caso C
    [Fact(Skip = BlockedReason)]
    public void Busqueda_by_phone_debe_localizar_el_cliente_esperado()
    {
        // Intencionalmente vacío: IClientRepository no expone GetByPhoneAsync
        // y ClientsController no expone un endpoint por teléfono.
    }

    // Caso D
    [Fact(Skip = BlockedReason)]
    public void Lookup_de_staff_debe_resolver_el_criterio_de_busqueda()
    {
        // Intencionalmente vacío: no existe entidad ni repositorio de "Staff"
        // en el dominio -- solo políticas de autorización con ese nombre.
    }
}
