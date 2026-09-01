using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.UserAccounts.UseCase;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.UserAccounts;

public sealed class UpdateUserAccountCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserAccountCommandHandler sut;

    public UpdateUserAccountCommandHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        sut = new UpdateUserAccountCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_mail_is_already_used_by_a_different_account()
    {
        var account = new UserAccountEntity(UserId, "ana", "ana@huellitas.test", "Activo");
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        userAccountsRepository.ExistsByUsernameAsync("ana", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("otra@huellitas.test", Arg.Any<CancellationToken>(), account.Id)
            .Returns(true);

        var command = new UpdateUserAccountCommand(account.Id, "ana", "otra@huellitas.test", "Activo");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await userAccountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_allows_keeping_the_accounts_own_current_mail()
    {
        var account = new UserAccountEntity(UserId, "ana", "ana@huellitas.test", "Activo");
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        userAccountsRepository.ExistsByUsernameAsync("ana", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);

        var command = new UpdateUserAccountCommand(account.Id, "ana", "ana@huellitas.test", "Inactivo");

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal("Inactivo", account.Status);
        await userAccountsRepository.Received(1).UpdateAsync(account, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
