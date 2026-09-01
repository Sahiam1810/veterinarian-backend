using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Security.ChangePassword;
using Application.UserCredentials.Abstraction;
using NSubstitute;
using Xunit;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.Tests.Security;

public sealed class ChangeMyPasswordCommandHandlerTests
{
    private static readonly Guid UserAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string CurrentPassword = "current-password";
    private const string NewPassword = "new-password-123";
    private const string StoredHash = "stored-hash";

    private readonly IUserCredentialsRepository userCredentialsRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ChangeMyPasswordCommandHandler sut;

    public ChangeMyPasswordCommandHandlerTests()
    {
        unitOfWork.UserCredentialsRepository.Returns(userCredentialsRepository);
        sut = new ChangeMyPasswordCommandHandler(unitOfWork, passwordHasher);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_authenticated_account_has_no_credentials_row()
    {
        userCredentialsRepository
            .GetByAccountIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns((UserCredentialsEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new ChangeMyPasswordCommand(UserAccountId, CurrentPassword, NewPassword),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_unauthorized_when_the_current_password_does_not_match()
    {
        var credentials = new UserCredentialsEntity(UserAccountId, StoredHash);
        userCredentialsRepository
            .GetByAccountIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(credentials);
        passwordHasher.Verify(CurrentPassword, StoredHash).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.Handle(
            new ChangeMyPasswordCommand(UserAccountId, CurrentPassword, NewPassword),
            CancellationToken.None));

        await userCredentialsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserCredentialsEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_updates_the_callers_own_credentials_when_the_current_password_matches()
    {
        var credentials = new UserCredentialsEntity(UserAccountId, StoredHash);
        userCredentialsRepository
            .GetByAccountIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(credentials);
        passwordHasher.Verify(CurrentPassword, StoredHash).Returns(true);
        passwordHasher.Hash(NewPassword).Returns("new-hash");

        await sut.Handle(
            new ChangeMyPasswordCommand(UserAccountId, CurrentPassword, NewPassword),
            CancellationToken.None);

        Assert.Equal("new-hash", credentials.PasswordHash);
        await userCredentialsRepository.Received(1).UpdateAsync(credentials, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
