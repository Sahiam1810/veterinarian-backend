using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Application.UserTokens.Abstraction;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.Tests.Users;

// P0 corregido: desactivar un usuario debía "revocar su acceso al sistema"
// (según el propio Swagger del endpoint) pero solo tocaba Users.IsActive,
// un campo que el login nunca lee (LoginAsync/RefreshAsync validan contra
// UserAccounts.Status). El usuario desactivado seguía pudiendo loguearse y
// sus refresh tokens seguían siendo válidos hasta expirar solos.
public sealed class DeactivateUserCommandHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserTokensRepository userTokensRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeactivateUserCommandHandler sut;

    public DeactivateUserCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
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
    public async Task Handle_marks_the_user_inactive_the_account_inactive_and_revokes_all_its_tokens()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        var account = new UserAccountEntity(user.Id, "ana", "ana@huellitas.test", "Activo");
        var tokenOne = new UserTokenEntity(account.Id, "hash-1", "refresh", DateTime.UtcNow.AddDays(1));
        var tokenTwo = new UserTokenEntity(account.Id, "hash-2", "refresh", DateTime.UtcNow.AddDays(2));

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        userAccountsRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(account);
        userTokensRepository.GetAllByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { tokenOne, tokenTwo });

        await sut.Handle(new DeactivateUserCommand(user.Id), CancellationToken.None);

        Assert.False(user.IsActive);
        Assert.Equal("Inactivo", account.Status);
        await userAccountsRepository.Received(1).UpdateAsync(account, Arg.Any<CancellationToken>());
        await userTokensRepository.Received(1).DeleteAsync(tokenOne, Arg.Any<CancellationToken>());
        await userTokensRepository.Received(1).DeleteAsync(tokenTwo, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_still_deactivates_the_user_when_it_has_no_linked_account()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        userAccountsRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        await sut.Handle(new DeactivateUserCommand(user.Id), CancellationToken.None);

        Assert.False(user.IsActive);
        await userAccountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
        await userTokensRepository.DidNotReceive().GetAllByAccountIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
