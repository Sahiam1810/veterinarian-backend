using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.Common;
using Domain.Veterinarians.Entities;
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

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly UpdateAppointmentCommandHandler sut;

    public UpdateAppointmentOwnershipTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        sut = new UpdateAppointmentCommandHandler(unitOfWork);
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
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UpdateAppointmentCommand CreateCommand(bool enforce) =>
        new(
            AppointmentId,
            ClientPetId,
            OwnVeterinarianId,
            ServiceId,
            StatusId,
            AvailabilityId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
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
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                "original"),
            AppointmentId);

    private static TEntity WithId<TEntity, TId>(TEntity entity, TId id)
        where TEntity : BaseEntity<TId>
    {
        typeof(BaseEntity<TId>).GetProperty(nameof(BaseEntity<TId>.Id))!.SetValue(entity, id);
        return entity;
    }
}
