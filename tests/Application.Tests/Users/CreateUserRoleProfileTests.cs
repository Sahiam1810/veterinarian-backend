using Application.Common.Abstractions;
using Application.Roles.Abstraction;
using Application.Specialties.Abstraction;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Application.Veterinarians.Abstraction;
using Domain.Specialties.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

// T10: CreateUser es solo personal; Veterinario sigue provisionando su perfil.
public sealed class CreateUserRoleProfileTests
{
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly ISpecialtyRepository specialtiesRepository = Substitute.For<ISpecialtyRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();

    public CreateUserRoleProfileTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        unitOfWork.SpecialtiesRepository.Returns(specialtiesRepository);
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        usersRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        veterinariansRepository.ExistsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        veterinariansRepository.ExistsByLicenseNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task Create_usuario_de_personal_hashea_password_y_no_crea_veterinario()
    {
        var staffRole = new RoleEntity("Recepcionista", "Gestiona agenda");
        rolesRepository.GetByIdAsync(staffRole.Id, Arg.Any<CancellationToken>()).Returns(staffRole);
        passwordHasher.Hash("Password123!").Returns("hash");

        UserEntity? persistedUser = null;
        usersRepository.AddAsync(Arg.Do<UserEntity>(u => persistedUser = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateUserCommandHandler(unitOfWork, passwordHasher);
        var userId = await sut.Handle(
            new CreateUserCommand(
                "Maria Recepcion",
                "maria@huellitas.test",
                Password: "Password123!",
                RoleId: staffRole.Id),
            CancellationToken.None);

        Assert.NotNull(persistedUser);
        Assert.Equal(userId, persistedUser!.Id);
        Assert.Equal("hash", persistedUser.PasswordHash);
        await veterinariansRepository.DidNotReceive()
            .AddAsync(Arg.Any<Veterinarian>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_veterinario_persiste_fila_Veterinarian_con_especialidad_y_licencia()
    {
        var vetRole = new RoleEntity("Veterinario", "Profesional");
        var specialty = new SpecialtyEntity("Cirugía", "Cirugía general");
        rolesRepository.GetByIdAsync(vetRole.Id, Arg.Any<CancellationToken>()).Returns(vetRole);
        specialtiesRepository.GetByIdAsync(specialty.Id, Arg.Any<CancellationToken>()).Returns(specialty);

        UserEntity? persistedUser = null;
        Veterinarian? persistedVet = null;
        usersRepository.AddAsync(Arg.Do<UserEntity>(u => persistedUser = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        veterinariansRepository.AddAsync(Arg.Do<Veterinarian>(v => persistedVet = v), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateUserCommandHandler(unitOfWork, passwordHasher);
        var userId = await sut.Handle(
            new CreateUserCommand(
                "Dr. Carlos",
                "vet@huellitas.test",
                "ValidPassword!1",
                vetRole.Id,
                SpecialtyId: specialty.Id,
                LicenseNumber: "CMP-998877"),
            CancellationToken.None);

        Assert.NotNull(persistedUser);
        Assert.Equal(userId, persistedUser!.Id);
        Assert.NotNull(persistedVet);
        Assert.Equal(userId, persistedVet!.UserId);
        Assert.Equal(specialty.Id, persistedVet.SpecialtyId);
        Assert.Equal("CMP-998877", persistedVet.LicenseNumber);
    }
}
