using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

// U5: USERS ya no tiene una cuenta separada que sincronizar al (re)activar.
public sealed class ActivateUserCommandHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ActivateUserCommandHandler sut;

    public ActivateUserCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
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
    public async Task Handle_marks_the_user_active()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        user.Deactivate();

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await sut.Handle(new ActivateUserCommand(user.Id), CancellationToken.None);

        Assert.True(user.IsActive);
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
