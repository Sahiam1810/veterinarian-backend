using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Security.Profile;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Security;

// U4: el sub ya es el id del usuario; el handler ya no pasa por UserAccounts.
public sealed class UpdateMyPhotoCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateMyPhotoCommandHandler sut;

    public UpdateMyPhotoCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new UpdateMyPhotoCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_user_does_not_exist()
    {
        usersRepository
            .GetByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((UserEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new UpdateMyPhotoCommand(UserId, "https://ejemplo.com/foto.jpg"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_persists_http_photo_url_on_the_authenticated_user()
    {
        var user = WithId(new UserEntity("Veterinario Prueba", "vet.prueba@veterinaria.com", "hash", RoleId), UserId);
        usersRepository
            .GetByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(user);

        const string photoUrl = "https://ejemplo.com/foto.jpg";
        await sut.Handle(
            new UpdateMyPhotoCommand(UserId, photoUrl),
            CancellationToken.None);

        Assert.Equal(photoUrl, user.PhotoUrl.Value);
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_clears_photo_when_url_is_empty()
    {
        var user = WithId(new UserEntity("Veterinario Prueba", "vet.prueba@veterinaria.com", "hash", RoleId), UserId);
        user.SetPhotoUrl("https://ejemplo.com/foto.jpg");
        usersRepository
            .GetByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(user);

        await sut.Handle(
            new UpdateMyPhotoCommand(UserId, "  "),
            CancellationToken.None);

        Assert.Null(user.PhotoUrl.Value);
    }

    private static UserEntity WithId(UserEntity entity, Guid id)
    {
        typeof(Domain.Common.BaseEntity<Guid>).GetProperty(nameof(Domain.Common.BaseEntity<Guid>.Id))!
            .SetValue(entity, id);
        return entity;
    }
}
