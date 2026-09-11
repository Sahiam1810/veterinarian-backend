using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.Common;
using Domain.Veterinarians.Entities;
using Domain.StatusAppointments.Entities;
using Application.StatusAppointments.Abstraction;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Appointments;

public sealed class UpdateAppointmentOwnershipTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorUserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OwnVeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ForeignVeterinarianId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ServiceId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid StatusId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid AvailabilityId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private static readonly DateTime MondaySlotStartUtc =
        new(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MondaySlotEndUtc =
        new(2026, 9, 7, 15, 0, 0, DateTimeKind.Utc);

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly Application.Availabilities.Abstraction.IAvailabilityRepository availabilitiesRepository
        = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
    private readonly IVeterinarianAbsenceRepository absences
        = Substitute.For<IVeterinarianAbsenceRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IStatusAppointmentRepository statusAppointmentsRepository
        = Substitute.For<IStatusAppointmentRepository>();
    private readonly UpdateAppointmentCommandHandler sut;

    public UpdateAppointmentOwnershipTests()
    {
        var client = new ClientEntity(Guid.NewGuid(), "1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Id))!.SetValue(clientPet, ClientPetId);

        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        unitOfWork.AvailabilitiesRepository.Returns(availabilitiesRepository);
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.StatusAppointmentsRepository.Returns(statusAppointmentsRepository);
        statusAppointmentsRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StatusAppointment("AGENDADA", null));
        clientPetsRepository.GetByIdAsync(ClientPetId, Arg.Any<CancellationToken>()).Returns(clientPet);
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        absences.GetOverlappingAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VeterinarianAbsence>());
        availabilitiesRepository.LockByIdAsync(AvailabilityId, Arg.Any<CancellationToken>())
            .Returns(new Availability(
                OwnVeterinarianId,
                DayOfWeek.Monday,
                new TimeOnly(8, 0),
                new TimeOnly(18, 0)));
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));
        sut = new UpdateAppointmentCommandHandler(unitOfWork, absences);
    }

    [Fact]
    public async Task OWN_PUT_T01_updates_when_veterinarian_owns_appointment()
    {
        var appointment = CreateAppointment(OwnVeterinarianId);
        ArrangeOwnedVeterinarian();
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        await sut.Handle(CreateCommand(enforce: true), CancellationToken.None);

        Assert.Equal("actualizado", appointment.Notes);
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_PUT_T02_throws_ForbiddenException_and_does_not_save_when_appointment_is_foreign()
    {
        var appointment = CreateAppointment(ForeignVeterinarianId);
        var originalNotes = appointment.Notes;
        ArrangeOwnedVeterinarian();
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(CreateCommand(enforce: true), CancellationToken.None));

        Assert.Equal(originalNotes, appointment.Notes);
        await appointmentsRepository.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_PUT_T03_throws_NotFoundException_and_does_not_persist_when_veterinarian_profile_is_missing()
    {
        var appointment = CreateAppointment(OwnVeterinarianId);
        var account = WithId(new UserAccountEntity(UserId, "vet", "vet@test.com", "Active"), ActorUserAccountId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        userAccountsRepository.GetByIdAsync(ActorUserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        veterinariansRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((Veterinarian?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(CreateCommand(enforce: true), CancellationToken.None));

        await appointmentsRepository.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_PUT_T04_skips_veterinarian_lookup_when_enforcement_is_disabled()
    {
        var appointment = CreateAppointment(ForeignVeterinarianId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        await sut.Handle(CreateCommand(enforce: false), CancellationToken.None);

        await userAccountsRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await veterinariansRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    private UpdateAppointmentCommand CreateCommand(bool enforce) =>
        new(
            AppointmentId,
            ClientPetId,
            OwnVeterinarianId,
            ServiceId,
            StatusId,
            AvailabilityId,
            MondaySlotStartUtc,
            MondaySlotEndUtc,
            "actualizado",
            ActorUserAccountId,
            enforce);

    private void ArrangeOwnedVeterinarian()
    {
        var account = WithId(new UserAccountEntity(UserId, "vet", "vet@test.com", "Active"), ActorUserAccountId);
        var veterinarian = WithId(new Veterinarian(UserId, Guid.NewGuid(), "LIC-001"), OwnVeterinarianId);
        userAccountsRepository.GetByIdAsync(ActorUserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        veterinariansRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);
    }

    private static Appointment CreateAppointment(Guid veterinarianId) =>
        WithId(
            new Appointment(
                ClientPetId,
                veterinarianId,
                ServiceId,
                StatusId,
                AvailabilityId,
                MondaySlotStartUtc,
                MondaySlotEndUtc,
                "original"),
            AppointmentId);

    private static TEntity WithId<TEntity, TId>(TEntity entity, TId id)
        where TEntity : BaseEntity<TId>
    {
        typeof(BaseEntity<TId>).GetProperty(nameof(BaseEntity<TId>.Id))!.SetValue(entity, id);
        return entity;
    }
}
