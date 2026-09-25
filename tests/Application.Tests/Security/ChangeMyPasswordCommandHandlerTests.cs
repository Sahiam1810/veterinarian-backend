using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Security.ChangePassword;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Security;

// U5: la contraseña vive directamente en USERS; ya no hay cuenta/credenciales
// separadas que resolver.
public sealed class ChangeMyPasswordCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const string CurrentPassword = "current-password";
    private const string NewPassword = "new-password-123";
    private const string StoredHash = "stored-hash";

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ChangeMyPasswordCommandHandler sut;

    public ChangeMyPasswordCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new ChangeMyPasswordCommandHandler(unitOfWork, passwordHasher);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_authenticated_user_does_not_exist()
    {
        usersRepository
            .GetByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((UserEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new ChangeMyPasswordCommand(UserId, CurrentPassword, NewPassword),
            CancellationToken.None));

        await usersRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_unauthorized_when_the_current_password_does_not_match()
    {
        ArrangeUser();
        passwordHasher.Verify(CurrentPassword, StoredHash).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.Handle(
            new ChangeMyPasswordCommand(UserId, CurrentPassword, NewPassword),
            CancellationToken.None));

        await usersRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_updates_the_callers_own_password_when_the_current_password_matches()
    {
        var user = ArrangeUser();
        passwordHasher.Verify(CurrentPassword, StoredHash).Returns(true);
        passwordHasher.Hash(NewPassword).Returns("new-hash");

        await sut.Handle(
            new ChangeMyPasswordCommand(UserId, CurrentPassword, NewPassword),
            CancellationToken.None);

        Assert.Equal("new-hash", user.PasswordHash);
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UserEntity ArrangeUser()
    {
        var user = new UserEntity("Usuario Prueba", "usuario@huellitas.test", StoredHash, RoleId);
        typeof(Domain.Common.BaseEntity<Guid>).GetProperty(nameof(Domain.Common.BaseEntity<Guid>.Id))!
            .SetValue(user, UserId);
        usersRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
