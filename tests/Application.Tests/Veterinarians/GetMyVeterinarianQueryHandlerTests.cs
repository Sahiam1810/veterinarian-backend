using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Veterinarians.Abstraction;
using Application.Veterinarians.UseCases;
using NSubstitute;
using Xunit;
using VeterinarianEntity = Domain.Veterinarians.Entities.Veterinarian;

namespace Application.Tests.Veterinarians;

// QA-VET-01: regresión del flujo GET /api/veterinarians/me.
// U4: JWT -> UserId -> Veterinarian por UserId directamente (ya no pasa por UserAccounts).
public sealed class GetMyVeterinarianQueryHandlerTests
{
    private static readonly Guid SpecialtyId = Guid.NewGuid();

    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetMyVeterinarianQueryHandler sut;

    public GetMyVeterinarianQueryHandlerTests()
    {
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        sut = new GetMyVeterinarianQueryHandler(unitOfWork);
    }

    // VET-T01: Veterinarian válido -> devuelve el perfil correcto.
    [Fact]
    public async Task Handle_returns_the_veterinarian_profile_when_it_exists()
    {
        var userId = Guid.NewGuid();
        var veterinarian = new VeterinarianEntity(userId, SpecialtyId, "LIC-001");

        veterinariansRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        var result = await sut.Handle(new GetMyVeterinarianQuery(userId), CancellationToken.None);

        Assert.Same(veterinarian, result);
        Assert.Equal(userId, result.UserId);
    }

    // VET-T03: no existe perfil Veterinarian para el usuario -> NotFoundException canónica.
    [Fact]
    public async Task Handle_throws_not_found_when_the_user_has_no_veterinarian_profile()
    {
        var userId = Guid.NewGuid();
        veterinariansRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((VeterinarianEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyVeterinarianQuery(userId), CancellationToken.None));
    }

    // VET-T04: la búsqueda del perfil debe hacerse directamente por el UserId de la
    // request, sin pasar por UserAccounts (retirado del flujo en U4).
    [Fact]
    public async Task Handle_looks_up_the_veterinarian_directly_by_the_requested_UserId()
    {
        var userId = Guid.NewGuid();
        var veterinarian = new VeterinarianEntity(userId, SpecialtyId, "LIC-002");

        veterinariansRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        await sut.Handle(new GetMyVeterinarianQuery(userId), CancellationToken.None);

        await veterinariansRepository.Received(1).GetByUserIdAsync(userId, Arg.Any<CancellationToken>());
    }

    // VET-T05: el mismo CancellationToken se propaga a la llamada de repositorio.
    [Fact]
    public async Task Handle_propagates_the_same_cancellation_token()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var userId = Guid.NewGuid();
        var veterinarian = new VeterinarianEntity(userId, SpecialtyId, "LIC-003");

        veterinariansRepository.GetByUserIdAsync(userId, token).Returns(veterinarian);

        await sut.Handle(new GetMyVeterinarianQuery(userId), token);

        await veterinariansRepository.Received(1).GetByUserIdAsync(userId, token);
    }

    // VET-T06: el flujo es de solo lectura -- no consulta un listado global ni persiste.
    [Fact]
    public async Task Handle_never_uses_a_global_lookup_or_persists_changes()
    {
        var userId = Guid.NewGuid();
        var veterinarian = new VeterinarianEntity(userId, SpecialtyId, "LIC-004");

        veterinariansRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        await sut.Handle(new GetMyVeterinarianQuery(userId), CancellationToken.None);

        await veterinariansRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
