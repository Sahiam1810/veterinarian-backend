using Application.AgentHumans.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.Abstraction;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Application.Veterinarians.Abstraction;
using Domain.AgentHumans.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

// T10: USERS es solo personal; borrar un usuario nunca consulta ni borra clientes.
public sealed class DeleteUserCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IAgentHumanRepository agentsRepository = Substitute.For<IAgentHumanRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly INotificationRepository notificationsRepository = Substitute.For<INotificationRepository>();

    public DeleteUserCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.AgentHumansRepository.Returns(agentsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        unitOfWork.NotificationsRepository.Returns(notificationsRepository);
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        agentsRepository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AgentHuman>());
        veterinariansRepository.GetIdByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
    }

    [Fact]
    public async Task Deleting_a_staff_user_removes_notifications_and_user_without_touching_clients()
    {
        var user = InactiveStaffUser();

        await new DeleteUserCommandHandler(unitOfWork)
            .Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await notificationsRepository.Received(1).DeleteByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
        await usersRepository.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
        AssertClientsWereNotTouched();
    }

    [Fact]
    public async Task Deleting_a_veterinarian_user_without_appointments_deletes_the_profile_without_touching_clients()
    {
        var user = InactiveStaffUser();
        var veterinarianId = Guid.NewGuid();
        veterinariansRepository.GetIdByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(veterinarianId);
        unitOfWork.AppointmentsRepository.GetByVeterinarianIdAsync(
                veterinarianId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Appointments.Entities.Appointment>());

        await new DeleteUserCommandHandler(unitOfWork)
            .Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await veterinariansRepository.Received(1).DeleteByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
        await usersRepository.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
        AssertClientsWereNotTouched();
    }

    [Fact]
    public async Task Deleting_an_active_user_is_rejected_before_anything_is_deleted()
    {
        var user = new UserEntity("Maria", "maria@huellitas.test", "hash", Guid.NewGuid());
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new DeleteUserCommandHandler(unitOfWork)
                .Handle(new DeleteUserCommand(user.Id), CancellationToken.None));

        await usersRepository.DidNotReceive().DeleteAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
        AssertClientsWereNotTouched();
    }

    private UserEntity InactiveStaffUser()
    {
        var user = new UserEntity("Maria", "maria@huellitas.test", "hash", Guid.NewGuid());
        user.Deactivate();
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private void AssertClientsWereNotTouched()
    {
        _ = unitOfWork.DidNotReceive().ClientsRepository;
        _ = unitOfWork.DidNotReceive().ClientPetsRepository;
    }
}
