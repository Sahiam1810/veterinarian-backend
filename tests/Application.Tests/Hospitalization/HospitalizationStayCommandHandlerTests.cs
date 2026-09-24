using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Appointments.Abstraction;
using Application.HospitalizationStays.Abstraction;
using Application.HospitalizationStays.UseCases;
using Application.Users.Abstraction;
using Domain.Appointments.Entities;
using Domain.HospitalizationStays.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Hospitalization;

public sealed class HospitalizationStayCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IHospitalizationStayRepository staysRepository = Substitute.For<IHospitalizationStayRepository>();
    private readonly IHospitalizationNoteRepository notesRepository = Substitute.For<IHospitalizationNoteRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();

    private readonly AdmitHospitalizationStayCommandHandler admitHandler;
    private readonly DischargeHospitalizationStayCommandHandler dischargeHandler;
    private readonly RegisterHospitalizationStayPaymentCommandHandler registerPaymentHandler;
    private readonly AddHospitalizationNoteCommandHandler addNoteHandler;
    private readonly GetHospitalizationNotesByStayQueryHandler getNotesHandler;


    private static readonly Guid ClientPetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AppointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AdmittingUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid AuthorUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid RecipientUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly SpeciesEntity DefaultSpecies = new("Perro");
    private static readonly RaceEntity DefaultRace = new("Labrador", DefaultSpecies);

    public HospitalizationStayCommandHandlerTests()
    {
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.HospitalizationStaysRepository.Returns(staysRepository);
        unitOfWork.HospitalizationNotesRepository.Returns(notesRepository);

        admitHandler = new AdmitHospitalizationStayCommandHandler(unitOfWork);
        dischargeHandler = new DischargeHospitalizationStayCommandHandler(unitOfWork);
        registerPaymentHandler = new RegisterHospitalizationStayPaymentCommandHandler(unitOfWork);
        addNoteHandler = new AddHospitalizationNoteCommandHandler(unitOfWork);
        getNotesHandler = new GetHospitalizationNotesByStayQueryHandler(unitOfWork);
    }


    [Fact]
    public async Task HOSPITALIZATION_T01_admit_stay_success_without_appointment()
    {
        clientPetsRepository.GetByIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns(new Domain.ClientsPets.Entities.ClientPetEntity(
                    new Domain.Clients.Entities.ClientEntity("Juan Pérez", "juan@vet.com", "12345678", "5551010", null),
                    new Domain.Pets.Entities.PetEntity("Perry", 5, "M", 12.5m, null, DefaultSpecies, DefaultRace),
                true));
        staysRepository.GetActiveByPetIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        var id = await admitHandler.Handle(
            new AdmitHospitalizationStayCommand(ClientPetId, null, "Observación inicial", AdmittingUserId),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await staysRepository.Received(1).AddAsync(Arg.Is<HospitalizationStay>(stay =>
            stay.ClientPetId == ClientPetId &&
            stay.AdmittedByUserId == AdmittingUserId &&
            stay.Motivo == "Observación inicial" &&
            stay.Estado == HospitalizationStayStatus.Activa), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HOSPITALIZATION_T02_admit_stay_success_with_appointment()
    {
        clientPetsRepository.GetByIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns(new Domain.ClientsPets.Entities.ClientPetEntity(
                    new Domain.Clients.Entities.ClientEntity("Juan Pérez", "juan@vet.com", "12345678", "5551010", null),
                    new Domain.Pets.Entities.PetEntity("Perry", 5, "M", 12.5m, null, DefaultSpecies, DefaultRace),
                true));
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(new Appointment(
                ClientPetId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null));
        staysRepository.GetActiveByPetIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        var id = await admitHandler.Handle(
            new AdmitHospitalizationStayCommand(ClientPetId, AppointmentId, "Ingreso por cita", AdmittingUserId),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await staysRepository.Received(1).AddAsync(Arg.Is<HospitalizationStay>(stay =>
            stay.AppointmentId == AppointmentId && stay.Motivo == "Ingreso por cita"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HOSPITALIZATION_T03_admit_stay_fails_when_pet_already_has_active_stay()
    {
        clientPetsRepository.GetByIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns(new Domain.ClientsPets.Entities.ClientPetEntity(
                    new Domain.Clients.Entities.ClientEntity("Juan Pérez", "juan@vet.com", "12345678", "5551010", null),
                    new Domain.Pets.Entities.PetEntity("Perry", 5, "M", 12.5m, null, DefaultSpecies, DefaultRace),
                true));
        staysRepository.GetActiveByPetIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns(new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Ingreso anterior"));

        await Assert.ThrowsAsync<ConflictException>(() => admitHandler.Handle(
            new AdmitHospitalizationStayCommand(ClientPetId, null, "Nuevo ingreso", AdmittingUserId),
            CancellationToken.None));
    }

    [Fact]
    public async Task HOSPITALIZATION_T04_discharge_stay_success()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        await dischargeHandler.Handle(new DischargeHospitalizationStayCommand(stay.Id), CancellationToken.None);

        Assert.Equal(HospitalizationStayStatus.DadaDeAlta, stay.Estado);
        Assert.NotNull(stay.FechaAlta);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HOSPITALIZATION_T05_discharge_stay_fails_if_already_discharged()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        stay.Discharge();
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dischargeHandler.Handle(new DischargeHospitalizationStayCommand(stay.Id), CancellationToken.None));
    }

    [Fact]
    public async Task HOSPITALIZATION_T06_add_note_success_without_recipient()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var id = await addNoteHandler.Handle(
            new AddHospitalizationNoteCommand(stay.Id, "Cambio de turno", null, AuthorUserId),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await notesRepository.Received(1).AddAsync(Arg.Is<HospitalizationNote>(note =>
            note.StayId == stay.Id &&
            note.AutorUserId == AuthorUserId &&
            note.Nota == "Cambio de turno" &&
            note.EntregadoAUserId == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HOSPITALIZATION_T07_add_note_success_with_recipient()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);
        usersRepository.GetByIdAsync(RecipientUserId, Arg.Any<CancellationToken>())
            .Returns(new Domain.Users.Entities.Users("Receptor", "receptor@vet.com", "hash-123", Guid.NewGuid()));

        var id = await addNoteHandler.Handle(
            new AddHospitalizationNoteCommand(stay.Id, "Entrega de turno", RecipientUserId, AuthorUserId),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await notesRepository.Received(1).AddAsync(Arg.Is<HospitalizationNote>(note =>
            note.EntregadoAUserId == RecipientUserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HOSPITALIZATION_T08_add_note_fails_when_stay_not_found()
    {
        staysRepository.GetByIdAsync(Guid.NewGuid(), Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            addNoteHandler.Handle(new AddHospitalizationNoteCommand(Guid.NewGuid(), "Nota", null, AuthorUserId), CancellationToken.None));
    }

    [Fact]
    public async Task HOSPITALIZATION_T09_add_note_fails_when_recipient_user_missing()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);
        usersRepository.GetByIdAsync(RecipientUserId, Arg.Any<CancellationToken>())
            .Returns((Domain.Users.Entities.Users?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            addNoteHandler.Handle(new AddHospitalizationNoteCommand(stay.Id, "Entrega de turno", RecipientUserId, AuthorUserId), CancellationToken.None));
    }

    [Fact]
    public async Task HOSPITALIZATION_T10_list_notes_returns_chronological_order()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        var note1 = new HospitalizationNote(stay.Id, AuthorUserId, "Primera", null);
        var note2 = new HospitalizationNote(stay.Id, AuthorUserId, "Segunda", null);
        typeof(HospitalizationNote).GetProperty(nameof(HospitalizationNote.FechaHora))!
            .SetValue(note1, DateTime.UtcNow.AddMinutes(-10));
        typeof(HospitalizationNote).GetProperty(nameof(HospitalizationNote.FechaHora))!
            .SetValue(note2, DateTime.UtcNow);

        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);
        notesRepository.GetByStayIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { note2, note1 });

        var result = await getNotesHandler.Handle(new GetHospitalizationNotesByStayQuery(stay.Id), CancellationToken.None);

        Assert.Collection(result,
            item => Assert.Equal("Primera", item.Nota),
            item => Assert.Equal("Segunda", item.Nota));
    }

    [Fact]
    public async Task HOSPITALIZATION_T11_register_payment_success()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        await registerPaymentHandler.Handle(new RegisterHospitalizationStayPaymentCommand(stay.Id), CancellationToken.None);

        Assert.True(stay.IsPaid);
        Assert.NotNull(stay.PaidAt);
        await staysRepository.Received(1).UpdateAsync(stay, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HOSPITALIZATION_T12_register_payment_fails_when_stay_not_found()
    {
        var nonExistentId = Guid.NewGuid();
        staysRepository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            registerPaymentHandler.Handle(new RegisterHospitalizationStayPaymentCommand(nonExistentId), CancellationToken.None));
    }

    [Fact]
    public async Task HOSPITALIZATION_T13_register_payment_fails_when_already_paid()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        stay.RegisterPayment();
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registerPaymentHandler.Handle(new RegisterHospitalizationStayPaymentCommand(stay.Id), CancellationToken.None));
        Assert.Equal("La estancia ya está pagada.", ex.Message);
    }

    [Fact]
    public void HOSPITALIZATION_T14_domain_register_payment_sets_is_paid_true_and_paid_at()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        Assert.False(stay.IsPaid);
        Assert.Null(stay.PaidAt);

        stay.RegisterPayment();

        Assert.True(stay.IsPaid);
        Assert.NotNull(stay.PaidAt);
    }

    [Fact]
    public void HOSPITALIZATION_T15_domain_register_payment_throws_if_already_paid()
    {
        var stay = new HospitalizationStay(ClientPetId, null, AdmittingUserId, "Observación");
        stay.RegisterPayment();

        var ex = Assert.Throws<InvalidOperationException>(() => stay.RegisterPayment());
        Assert.Equal("La estancia ya está pagada.", ex.Message);
    }

    [Fact]
    public async Task HOSPITALIZATION_T16_admit_stay_concurrent_requests_handled_safely()
    {
        clientPetsRepository.GetByIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns(new Domain.ClientsPets.Entities.ClientPetEntity(
                    new Domain.Clients.Entities.ClientEntity("Juan Pérez", "juan@vet.com", "12345678", "5551010", null),
                    new Domain.Pets.Entities.PetEntity("Perry", 5, "M", 12.5m, null, DefaultSpecies, DefaultRace),
                true));

        staysRepository.GetActiveByPetIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        var task1 = admitHandler.Handle(new AdmitHospitalizationStayCommand(ClientPetId, null, "Ingreso A", AdmittingUserId), CancellationToken.None);
        var task2 = admitHandler.Handle(new AdmitHospitalizationStayCommand(ClientPetId, null, "Ingreso B", AdmittingUserId), CancellationToken.None);

        var results = await Task.WhenAll(task1, task2);

        Assert.Equal(2, results.Length);
        Assert.NotEqual(Guid.Empty, results[0]);
        Assert.NotEqual(Guid.Empty, results[1]);
    }
}


