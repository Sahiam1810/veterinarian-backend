using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.Common;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

// U4: el ownership ya resuelve el veterinario directamente por el UserId del
// actor (el sub del JWT), sin pasar por UserAccounts.
public sealed class GetAppointmentByIdOwnershipTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OwnVeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ForeignVeterinarianId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly GetAppointmentByIdQueryHandler sut;

    public GetAppointmentByIdOwnershipTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        sut = new GetAppointmentByIdQueryHandler(unitOfWork);
    }

    [Fact]
    public async Task OWN_GET_T01_returns_appointment_when_veterinarian_owns_it()
    {
        var appointment = CreateAppointment(OwnVeterinarianId);
        ArrangeOwnedVeterinarian();
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var result = await sut.Handle(
            new GetAppointmentByIdQuery(AppointmentId, ActorUserId, EnforceVeterinarianOwnership: true),
            CancellationToken.None);

        Assert.Same(appointment, result);
    }

    [Fact]
    public async Task OWN_GET_T02_throws_ForbiddenException_when_appointment_belongs_to_another_veterinarian()
    {
        var appointment = CreateAppointment(ForeignVeterinarianId);
        ArrangeOwnedVeterinarian();
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(
            new GetAppointmentByIdQuery(AppointmentId, ActorUserId, EnforceVeterinarianOwnership: true),
            CancellationToken.None));
    }

    [Fact]
    public async Task OWN_GET_T03_throws_NotFoundException_when_veterinarian_profile_is_missing()
    {
        var appointment = CreateAppointment(OwnVeterinarianId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        veterinariansRepository.GetByUserIdAsync(ActorUserId, Arg.Any<CancellationToken>())
            .Returns((Veterinarian?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new GetAppointmentByIdQuery(AppointmentId, ActorUserId, EnforceVeterinarianOwnership: true),
            CancellationToken.None));
    }

    [Fact]
    public async Task OWN_GET_T04_skips_veterinarian_lookup_when_enforcement_is_disabled()
    {
        var appointment = CreateAppointment(ForeignVeterinarianId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var result = await sut.Handle(
            new GetAppointmentByIdQuery(AppointmentId, ActorUserId, EnforceVeterinarianOwnership: false),
            CancellationToken.None);

        Assert.Same(appointment, result);
        await veterinariansRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OWN_GET_T05_throws_NotFoundException_when_appointment_is_missing()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new GetAppointmentByIdQuery(AppointmentId, ActorUserId, EnforceVeterinarianOwnership: true),
            CancellationToken.None));

        await veterinariansRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeOwnedVeterinarian()
    {
        var veterinarian = WithId(new Veterinarian(ActorUserId, Guid.NewGuid(), "LIC-001"), OwnVeterinarianId);
        veterinariansRepository.GetByUserIdAsync(ActorUserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);
    }

    private static Appointment CreateAppointment(Guid veterinarianId) =>
        WithId(
            new Appointment(
                Guid.NewGuid(),
                veterinarianId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null),
            AppointmentId);

    private static TEntity WithId<TEntity, TId>(TEntity entity, TId id)
        where TEntity : BaseEntity<TId>
    {
        typeof(BaseEntity<TId>).GetProperty(nameof(BaseEntity<TId>.Id))!.SetValue(entity, id);
        return entity;
    }
}
