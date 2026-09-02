using Application.AppointmentStatusHistories.Abstraction;
using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.AppointmentStatusHistories.Entities;
using Domain.Appointments.Entities;
using Domain.Common;
using Domain.StatusAppointments.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Appointments;

public sealed class UpdateAppointmentStatusOwnershipTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorUserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OwnVeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ForeignVeterinarianId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AgendadaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AtendidaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IStatusAppointmentRepository statusAppointmentsRepository = Substitute.For<IStatusAppointmentRepository>();
    private readonly IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository =
        Substitute.For<IAppointmentStatusHistoryRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly UpdateAppointmentStatusCommandHandler sut;

    public UpdateAppointmentStatusOwnershipTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.StatusAppointmentsRepository.Returns(statusAppointmentsRepository);
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(appointmentStatusHistoriesRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        sut = new UpdateAppointmentStatusCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task OWN_STA_T01_updates_status_when_veterinarian_owns_appointment()
    {
        var appointment = ArrangeTransition(OwnVeterinarianId);
        ArrangeOwnedVeterinarian();

        await sut.Handle(
            new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null, ActorUserAccountId, true),
            CancellationToken.None);

        Assert.Equal(AtendidaId, appointment.StatusId);
        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_STA_T02_throws_ForbiddenException_before_history_when_appointment_is_foreign()
    {
        ArrangeTransition(ForeignVeterinarianId);
        ArrangeOwnedVeterinarian();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(
            new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null, ActorUserAccountId, true),
            CancellationToken.None));

        await appointmentStatusHistoriesRepository.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await appointmentsRepository.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_STA_T03_throws_NotFoundException_without_persistence_when_veterinarian_profile_is_missing()
    {
        ArrangeTransition(OwnVeterinarianId);
        var account = WithId(new UserAccountEntity(UserId, "vet", "vet@test.com", "Active"), ActorUserAccountId);
        userAccountsRepository.GetByIdAsync(ActorUserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        veterinariansRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((Veterinarian?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null, ActorUserAccountId, true),
            CancellationToken.None));

        await appointmentStatusHistoriesRepository.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await appointmentsRepository.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_STA_T04_skips_veterinarian_lookup_when_enforcement_is_disabled()
    {
        var appointment = ArrangeTransition(ForeignVeterinarianId);

        await sut.Handle(
            new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null, ActorUserAccountId, false),
            CancellationToken.None);

        Assert.Equal(AtendidaId, appointment.StatusId);
        await userAccountsRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await veterinariansRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Appointment ArrangeTransition(Guid veterinarianId)
    {
        var appointment = WithId(
            new Appointment(
                ClientPetId,
                veterinarianId,
                Guid.NewGuid(),
                AgendadaId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null),
            AppointmentId);
        var currentStatus = CreateStatus("AGENDADA", AgendadaId);
        var targetStatus = CreateStatus("ATENDIDA", AtendidaId);

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(AgendadaId, Arg.Any<CancellationToken>())
            .Returns(currentStatus);
        statusAppointmentsRepository.GetByIdAsync(AtendidaId, Arg.Any<CancellationToken>())
            .Returns(targetStatus);

        return appointment;
    }

    private void ArrangeOwnedVeterinarian()
    {
        var account = WithId(new UserAccountEntity(UserId, "vet", "vet@test.com", "Active"), ActorUserAccountId);
        var veterinarian = WithId(new Veterinarian(UserId, Guid.NewGuid(), "LIC-001"), OwnVeterinarianId);
        userAccountsRepository.GetByIdAsync(ActorUserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        veterinariansRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);
    }

    private static StatusAppointment CreateStatus(string name, Guid id)
    {
        var status = new StatusAppointment(name, null);
        typeof(StatusAppointment)
            .GetProperty(nameof(StatusAppointment.Id))!
            .SetValue(status, id);
        return status;
    }

    private static TEntity WithId<TEntity, TId>(TEntity entity, TId id)
        where TEntity : BaseEntity<TId>
    {
        typeof(BaseEntity<TId>).GetProperty(nameof(BaseEntity<TId>.Id))!.SetValue(entity, id);
        return entity;
    }
}
