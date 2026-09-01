using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

public sealed class ActivateUserCommandHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ActivateUserCommandHandler sut;

    public ActivateUserCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        sut = new ActivateUserCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_user_does_not_exist()
    {
        var userId = Guid.NewGuid();
        usersRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new ActivateUserCommand(userId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_marks_the_user_active_and_restores_the_account_status_to_activo()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        user.Deactivate();
        var account = new UserAccountEntity(user.Id, "ana", "ana@huellitas.test", "Inactivo");

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        userAccountsRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(account);

        await sut.Handle(new ActivateUserCommand(user.Id), CancellationToken.None);

        Assert.True(user.IsActive);
        Assert.Equal("Activo", account.Status);
        await userAccountsRepository.Received(1).UpdateAsync(account, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_still_activates_the_user_when_it_has_no_linked_account()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        user.Deactivate();

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        userAccountsRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        await sut.Handle(new ActivateUserCommand(user.Id), CancellationToken.None);

        Assert.True(user.IsActive);
        await userAccountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
    }
}
