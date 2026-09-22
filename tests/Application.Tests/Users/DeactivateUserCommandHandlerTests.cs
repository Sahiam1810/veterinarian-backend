using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Application.UserTokens.Abstraction;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.Tests.Users;

// P0 corregido: desactivar un usuario debía "revocar su acceso al sistema"
// (según el propio Swagger del endpoint) pero solo tocaba Users.IsActive,
// un campo que el login nunca leía (LoginAsync/RefreshAsync validaban contra
// UserAccounts.Status). El usuario desactivado seguía pudiendo loguearse y
// sus refresh tokens seguían siendo válidos hasta expirar solos.
// U5: USERS ya es la única fuente de verdad, así que login y desactivación
// leen el mismo campo (User.IsActive); no queda una cuenta separada que
// sincronizar.
public sealed class DeactivateUserCommandHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUserTokensRepository userTokensRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeactivateUserCommandHandler sut;

    public DeactivateUserCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.UserTokensRepository.Returns(userTokensRepository);
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        sut = new DeactivateUserCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_user_does_not_exist()
    {
        var userId = Guid.NewGuid();
        usersRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new DeactivateUserCommand(userId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_marks_the_user_inactive_and_revokes_all_its_tokens()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        var sessionStartedAt = DateTime.UtcNow;
        var tokenOne = new UserTokenEntity(user.Id, "hash-1", "refresh", DateTime.UtcNow.AddDays(1), sessionStartedAt);
        var tokenTwo = new UserTokenEntity(user.Id, "hash-2", "refresh", DateTime.UtcNow.AddDays(2), sessionStartedAt);

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        userTokensRepository.GetAllByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { tokenOne, tokenTwo });

        await sut.Handle(new DeactivateUserCommand(user.Id), CancellationToken.None);

        Assert.False(user.IsActive);
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await userTokensRepository.Received(1).DeleteAsync(tokenOne, Arg.Any<CancellationToken>());
        await userTokensRepository.Received(1).DeleteAsync(tokenTwo, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
