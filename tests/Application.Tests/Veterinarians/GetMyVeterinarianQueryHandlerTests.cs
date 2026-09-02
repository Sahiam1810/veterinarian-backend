using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.Veterinarians.Abstraction;
using Application.Veterinarians.UseCases;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using VeterinarianEntity = Domain.Veterinarians.Entities.Veterinarian;

namespace Application.Tests.Veterinarians;

// QA-VET-01: regresión del flujo GET /api/veterinarians/me.
// JWT -> UserAccountId -> UserAccount -> account.UserId -> Veterinarian por UserId.
public sealed class GetMyVeterinarianQueryHandlerTests
{
    private static readonly Guid SpecialtyId = Guid.NewGuid();

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetMyVeterinarianQueryHandler sut;

    public GetMyVeterinarianQueryHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        sut = new GetMyVeterinarianQueryHandler(unitOfWork);
    }

    // VET-T01: UserAccount y Veterinarian válidos -> devuelve el perfil correcto.
    [Fact]
    public async Task Handle_returns_the_veterinarian_profile_when_account_and_profile_exist()
    {
        var account = new UserAccountEntity(Guid.NewGuid(), "drana", "drana@huellitas.test", "Activo");
        var veterinarian = new VeterinarianEntity(account.UserId, SpecialtyId, "LIC-001");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        veterinariansRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        var result = await sut.Handle(new GetMyVeterinarianQuery(account.Id), CancellationToken.None);

        Assert.Same(veterinarian, result);
        Assert.Equal(account.UserId, result.UserId);
    }

    // VET-T02: no existe UserAccount para la identidad solicitada -> NotFoundException canónica.
    [Fact]
    public async Task Handle_throws_not_found_when_the_user_account_does_not_exist()
    {
        var userAccountId = Guid.NewGuid();
        userAccountsRepository.GetByIdAsync(userAccountId, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyVeterinarianQuery(userAccountId), CancellationToken.None));

        await veterinariansRepository.DidNotReceive().GetByUserIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // VET-T03: existe UserAccount, pero no tiene perfil Veterinarian -> NotFoundException canónica.
    [Fact]
    public async Task Handle_throws_not_found_when_the_account_has_no_veterinarian_profile()
    {
        var account = new UserAccountEntity(Guid.NewGuid(), "recepcion", "recepcion@huellitas.test", "Activo");
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        veterinariansRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns((VeterinarianEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyVeterinarianQuery(account.Id), CancellationToken.None));
    }

    // VET-T04: la búsqueda del perfil debe hacerse por account.UserId, nunca por el
    // UserAccountId de la request -- son valores deliberadamente distintos en este test.
    [Fact]
    public async Task Handle_looks_up_the_veterinarian_by_account_UserId_not_by_the_requested_UserAccountId()
    {
        var account = new UserAccountEntity(Guid.NewGuid(), "drbeto", "drbeto@huellitas.test", "Activo");
        var veterinarian = new VeterinarianEntity(account.UserId, SpecialtyId, "LIC-002");

        Assert.NotEqual(account.Id, account.UserId);

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        veterinariansRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        await sut.Handle(new GetMyVeterinarianQuery(account.Id), CancellationToken.None);

        await veterinariansRepository.Received(1).GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>());
        await veterinariansRepository.DidNotReceive().GetByUserIdAsync(account.Id, Arg.Any<CancellationToken>());
    }

    // VET-T05: el mismo CancellationToken se propaga a cada llamada de repositorio,
    // tanto en el flujo válido como en los flujos de error.
    [Fact]
    public async Task Handle_propagates_the_same_cancellation_token_to_every_repository_call()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var account = new UserAccountEntity(Guid.NewGuid(), "drcarla", "drcarla@huellitas.test", "Activo");
        var veterinarian = new VeterinarianEntity(account.UserId, SpecialtyId, "LIC-003");

        userAccountsRepository.GetByIdAsync(account.Id, token).Returns(account);
        veterinariansRepository.GetByUserIdAsync(account.UserId, token).Returns(veterinarian);

        await sut.Handle(new GetMyVeterinarianQuery(account.Id), token);

        await userAccountsRepository.Received(1).GetByIdAsync(account.Id, token);
        await veterinariansRepository.Received(1).GetByUserIdAsync(account.UserId, token);
    }

    [Fact]
    public async Task Handle_propagates_the_cancellation_token_when_the_account_does_not_exist()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userAccountId = Guid.NewGuid();

        userAccountsRepository.GetByIdAsync(userAccountId, token).Returns((UserAccountEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyVeterinarianQuery(userAccountId), token));

        await userAccountsRepository.Received(1).GetByIdAsync(userAccountId, token);
    }

    // VET-T06: el flujo es de solo lectura -- no consulta un listado global ni persiste.
    [Fact]
    public async Task Handle_never_uses_a_global_lookup_or_persists_changes()
    {
        var account = new UserAccountEntity(Guid.NewGuid(), "drdiego", "drdiego@huellitas.test", "Activo");
        var veterinarian = new VeterinarianEntity(account.UserId, SpecialtyId, "LIC-004");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        veterinariansRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        await sut.Handle(new GetMyVeterinarianQuery(account.Id), CancellationToken.None);

        await veterinariansRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
