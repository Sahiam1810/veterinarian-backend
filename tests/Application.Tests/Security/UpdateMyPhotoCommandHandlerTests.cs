using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Security.Profile;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Security;

public sealed class UpdateMyPhotoCommandHandlerTests
{
    private static readonly Guid UserAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateMyPhotoCommandHandler sut;

    public UpdateMyPhotoCommandHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new UpdateMyPhotoCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_account_does_not_exist()
    {
        userAccountsRepository
            .GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new UpdateMyPhotoCommand(UserAccountId, "https://ejemplo.com/foto.jpg"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_persists_http_photo_url_on_the_authenticated_user()
    {
        var user = new UserEntity("Veterinario Prueba", "vet.prueba@veterinaria.com", null, RoleId);
        var account = new UserAccountEntity(user.Id, "vetprueba", "vet.prueba@veterinaria.com", "Activo");
        userAccountsRepository
            .GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        usersRepository
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        const string photoUrl = "https://ejemplo.com/foto.jpg";
        await sut.Handle(
            new UpdateMyPhotoCommand(UserAccountId, photoUrl),
            CancellationToken.None);

        Assert.Equal(photoUrl, user.PhotoUrl.Value);
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_clears_photo_when_url_is_empty()
    {
        var user = new UserEntity("Veterinario Prueba", "vet.prueba@veterinaria.com", null, RoleId);
        user.SetPhotoUrl("https://ejemplo.com/foto.jpg");
        var account = new UserAccountEntity(user.Id, "vetprueba", "vet.prueba@veterinaria.com", "Activo");
        userAccountsRepository
            .GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        usersRepository
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        await sut.Handle(
            new UpdateMyPhotoCommand(UserAccountId, "  "),
            CancellationToken.None);

        Assert.Null(user.PhotoUrl.Value);
    }
}
