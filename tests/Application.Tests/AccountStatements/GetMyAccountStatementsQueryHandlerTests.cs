using Application.AccountStatements.Abstraction;
using Application.AccountStatements.UseCases;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Domain.Clients.Entities;
using Domain.Common;
using NSubstitute;
using Xunit;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.AccountStatements;

public sealed class GetMyAccountStatementsQueryHandlerTests
{
    private static readonly Guid UserIdA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserIdB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserAccountIdA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserAccountIdB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IAccountStatementsRepository accountStatementsRepository = Substitute.For<IAccountStatementsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetMyAccountStatementsQueryHandler sut;

    public GetMyAccountStatementsQueryHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.AccountStatementsRepository.Returns(accountStatementsRepository);
        sut = new GetMyAccountStatementsQueryHandler(unitOfWork);
    }

    private static UserAccountEntity CreateUserAccount(Guid id, Guid userId)
        => WithId(new UserAccountEntity(userId, "user", "user@example.com", "Active"), id);

    private static ClientEntity CreateClient(Guid userId, string identificationNumber)
        => WithId(new ClientEntity(userId, identificationNumber, "Address"), Guid.NewGuid());

    private static TEntity WithId<TEntity, TId>(TEntity entity, TId id)
        where TEntity : BaseEntity<TId>
    {
        typeof(BaseEntity<TId>).GetProperty(nameof(BaseEntity<TId>.Id))!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Handle_DEC_18_T01_client_with_own_statements_returns_only_own()
    {
        // Arrange
        var account = CreateUserAccount(UserAccountIdA, UserIdA);
        var client = CreateClient(UserIdA, "ID-A");
        var ownStatement = new AccountStatementEntity(UserAccountIdA, DateTime.UtcNow, "Pending");
        var otherStatement = new AccountStatementEntity(UserAccountIdB, DateTime.UtcNow, "Paid");

        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(account);

        clientsRepository
            .GetByUserIdAsync(UserIdA, Arg.Any<CancellationToken>())
            .Returns(client);

        accountStatementsRepository
            .GetAllByAccountIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(new[] { ownStatement });

        // Act
        var result = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(ownStatement.Id, result.First().Id);
        Assert.Equal(UserAccountIdA, result.First().AccountId);
        Assert.DoesNotContain(result, statement => statement.AccountId == otherStatement.AccountId);
    }

    [Fact]
    public async Task Handle_DEC_18_T02_no_client_profile_returns_empty()
    {
        // Arrange
        var account = CreateUserAccount(UserAccountIdA, UserIdA);

        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(account);

        clientsRepository
            .GetByUserIdAsync(UserIdA, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        // Act
        var result = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_DEC_18_T03_client_a_vs_b_isolation()
    {
        // Arrange
        var accountA = CreateUserAccount(UserAccountIdA, UserIdA);
        var accountB = CreateUserAccount(UserAccountIdB, UserIdB);
        var clientA = CreateClient(UserIdA, "ID-A");
        var clientB = CreateClient(UserIdB, "ID-B");
        var statementA = new AccountStatementEntity(UserAccountIdA, DateTime.UtcNow.AddDays(-1), "Pending");
        var statementB = new AccountStatementEntity(UserAccountIdB, DateTime.UtcNow, "Paid");

        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(accountA);

        clientsRepository
            .GetByUserIdAsync(UserIdA, Arg.Any<CancellationToken>())
            .Returns(clientA);

        accountStatementsRepository
            .GetAllByAccountIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(new[] { statementA });

        userAccountsRepository
            .GetByIdAsync(UserAccountIdB, Arg.Any<CancellationToken>())
            .Returns(accountB);

        clientsRepository
            .GetByUserIdAsync(UserIdB, Arg.Any<CancellationToken>())
            .Returns(clientB);

        accountStatementsRepository
            .GetAllByAccountIdAsync(UserAccountIdB, Arg.Any<CancellationToken>())
            .Returns(new[] { statementB });

        // Act
        var resultA = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), CancellationToken.None);
        var resultB = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdB), CancellationToken.None);

        // Assert
        Assert.Single(resultA);
        Assert.Equal(statementA.Id, resultA.First().Id);
        Assert.Single(resultB);
        Assert.Equal(statementB.Id, resultB.First().Id);
        Assert.DoesNotContain(resultA, statement => statement.Id == statementB.Id);
        Assert.DoesNotContain(resultB, statement => statement.Id == statementA.Id);
    }

    [Fact]
    public async Task Handle_DEC_18_T04_client_with_zero_statements_returns_empty()
    {
        // Arrange
        var account = CreateUserAccount(UserAccountIdA, UserIdA);
        var client = CreateClient(UserIdA, "ID-A");

        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(account);

        clientsRepository
            .GetByUserIdAsync(UserIdA, Arg.Any<CancellationToken>())
            .Returns(client);

        accountStatementsRepository
            .GetAllByAccountIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AccountStatementEntity>());

        // Act
        var result = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_DEC_18_T05_no_client_does_not_call_get_all_by_account_id()
    {
        // Arrange
        var account = CreateUserAccount(UserAccountIdA, UserIdA);

        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns(account);

        clientsRepository
            .GetByUserIdAsync(UserIdA, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        // Act
        var result = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), CancellationToken.None);

        // Assert
        Assert.Empty(result);
        await accountStatementsRepository
            .DidNotReceive()
            .GetAllByAccountIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DEC_18_T06_propagates_cancellation_token()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var account = CreateUserAccount(UserAccountIdA, UserIdA);
        var client = CreateClient(UserIdA, "ID-A");
        var statement = new AccountStatementEntity(UserAccountIdA, DateTime.UtcNow, "Pending");

        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, cancellationToken)
            .Returns(account);

        clientsRepository
            .GetByUserIdAsync(UserIdA, cancellationToken)
            .Returns(client);

        accountStatementsRepository
            .GetAllByAccountIdAsync(UserAccountIdA, cancellationToken)
            .Returns(new[] { statement });

        // Act
        var result = await sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), cancellationToken);

        // Assert
        Assert.Single(result);
        await userAccountsRepository.Received(1).GetByIdAsync(UserAccountIdA, cancellationToken);
        await clientsRepository.Received(1).GetByUserIdAsync(UserIdA, cancellationToken);
        await accountStatementsRepository.Received(1).GetAllByAccountIdAsync(UserAccountIdA, cancellationToken);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_user_account_does_not_exist()
    {
        // Arrange
        userAccountsRepository
            .GetByIdAsync(UserAccountIdA, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyAccountStatementsQuery(UserAccountIdA), CancellationToken.None));
    }
}
