using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Domain.Roles;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

// Reemplaza al antiguo UserCredentialsController.ChangePassword (SEC-02):
// exclusivo de SuperAdmin, no requiere la contraseña actual del usuario
// destino (recuperación de acceso, no autoservicio).
public sealed class ResetUserPasswordCommandHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ResetUserPasswordCommandHandler sut;

    public ResetUserPasswordCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new ResetUserPasswordCommandHandler(unitOfWork, passwordHasher);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_user_does_not_exist()
    {
        var userId = Guid.NewGuid();
        usersRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new ResetUserPasswordCommand(userId, "new-password-123"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_forbidden_when_the_target_user_is_SuperAdmin()
    {
        var user = new UserEntity("Root", "root@huellitas.test", "hash", SystemRoles.SuperAdminId);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(
            new ResetUserPasswordCommand(user.Id, "new-password-123"),
            CancellationToken.None));

        await usersRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_replaces_the_password_without_checking_the_current_one()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "old-hash", RoleId);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Hash("new-password-123").Returns("new-hash");

        await sut.Handle(
            new ResetUserPasswordCommand(user.Id, "new-password-123"),
            CancellationToken.None);

        Assert.Equal("new-hash", user.PasswordHash);
        passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
