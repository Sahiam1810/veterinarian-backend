using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Diagnostics.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.MedicalRecords.UseCases;
using Application.UserAccounts.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.Common;
using Domain.Diagnostics.Entities;
using Domain.MedicalRecords.Entities;
using Domain.Vaccinations.Entities;
using Domain.Veterinarians.Entities;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.MedicalRecords;

public sealed class CreateAppointmentMedicalRecordCommandHandlerTests
{
    private static readonly Guid ActorUserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ClientPetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AppointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OwnVeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ForeignVeterinarianId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DiagnosticId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly IDiagnosticRepository diagnosticsRepository = Substitute.For<IDiagnosticRepository>();
    private readonly IMedicalRecordRepository medicalRecordsRepository = Substitute.For<IMedicalRecordRepository>();
    private readonly IVaccinationRepository vaccinationsRepository = Substitute.For<IVaccinationRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateAppointmentMedicalRecordCommandHandler sut;
    private readonly CreateAppointmentMedicalRecordCommandValidator validator = new();

    public CreateAppointmentMedicalRecordCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        unitOfWork.DiagnosticsRepository.Returns(diagnosticsRepository);
        unitOfWork.MedicalRecordsRepository.Returns(medicalRecordsRepository);
        unitOfWork.VaccinationsRepository.Returns(vaccinationsRepository);
        sut = new CreateAppointmentMedicalRecordCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task MR_T01_creates_medical_record_without_vaccinations_for_owning_veterinarian()
    {
        ArrangeOwnedAppointment();
        ArrangeActiveDiagnostic();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        MedicalRecord? added = null;
        medicalRecordsRepository
            .When(x => x.AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => added = ci.Arg<MedicalRecord>());

        var result = await sut.Handle(CreateCommand(vaccinations: null), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(ClientPetId, added!.ClientPetId);
        Assert.Equal(AppointmentId, added.AppointmentId);
        Assert.Equal(DiagnosticId, added.DiagnosticId);
        Assert.Equal(added.Id, result.MedicalRecordId);
        Assert.Equal(AppointmentId, result.AppointmentId);
        Assert.Empty(result.VaccinationIds);
        await vaccinationsRepository.DidNotReceive()
            .AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MR_T02_creates_medical_record_and_vaccinations_atomically()
    {
        ArrangeOwnedAppointment();
        ArrangeActiveDiagnostic();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        MedicalRecord? addedRecord = null;
        medicalRecordsRepository
            .When(x => x.AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => addedRecord = ci.Arg<MedicalRecord>());

        var capturedVaccinations = new List<Vaccination>();
        vaccinationsRepository
            .When(x => x.AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedVaccinations.Add(ci.Arg<Vaccination>()));

        var applicationDate = DateTime.UtcNow.Date;
        var vaccinations = new[]
        {
            new CreateAppointmentMedicalRecordVaccinationItem("Rabia", 1, applicationDate, applicationDate.AddYears(1)),
            new CreateAppointmentMedicalRecordVaccinationItem("Parvo", 2, applicationDate, null)
        };

        var result = await sut.Handle(CreateCommand(vaccinations), CancellationToken.None);

        Assert.NotNull(addedRecord);
        Assert.Equal(2, capturedVaccinations.Count);
        Assert.All(capturedVaccinations, v =>
        {
            Assert.Equal(ClientPetId, v.ClientPetId);
            Assert.Equal(addedRecord!.Id, v.RecordId);
        });
        Assert.Equal(capturedVaccinations.Select(v => v.Id).ToArray(), result.VaccinationIds);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MR_T03_throws_NotFoundException_when_appointment_is_missing()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(CreateCommand(), CancellationToken.None));

        await AssertNoPersistenceAsync();
    }

    [Fact]
    public async Task MR_T04_throws_NotFoundException_when_veterinarian_profile_is_missing()
    {
        ArrangeAppointment(OwnVeterinarianId);
        var account = WithId(new UserAccountEntity(UserId, "vet", "vet@test.com", "Active"), ActorUserAccountId);
        userAccountsRepository.GetByIdAsync(ActorUserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        veterinariansRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((Veterinarian?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(CreateCommand(enforce: true), CancellationToken.None));

        await AssertNoPersistenceAsync();
    }

    [Fact]
    public async Task MR_T05_throws_ForbiddenException_when_appointment_is_foreign()
    {
        ArrangeAppointment(ForeignVeterinarianId);
        ArrangeOwnedVeterinarian();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(CreateCommand(enforce: true), CancellationToken.None));

        await AssertNoPersistenceAsync();
    }

    [Fact]
    public async Task MR_T06_throws_ConflictException_when_medical_record_already_exists()
    {
        ArrangeOwnedAppointment();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.Handle(CreateCommand(), CancellationToken.None));

        await medicalRecordsRepository.DidNotReceive()
            .AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>());
        await vaccinationsRepository.DidNotReceive()
            .AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MR_T07_throws_NotFoundException_when_diagnostic_is_missing()
    {
        ArrangeOwnedAppointment();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(false);
        diagnosticsRepository.GetByIdAsync(DiagnosticId, Arg.Any<CancellationToken>())
            .Returns((Diagnostic?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(CreateCommand(), CancellationToken.None));

        await AssertNoPersistenceAsync();
    }

    [Fact]
    public async Task MR_T08_throws_BadRequestException_when_diagnostic_is_inactive()
    {
        ArrangeOwnedAppointment();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(false);
        diagnosticsRepository.GetByIdAsync(DiagnosticId, Arg.Any<CancellationToken>())
            .Returns(new Diagnostic
            {
                Id = DiagnosticId,
                Code = "DX-01",
                Name = "Inactivo",
                IsActive = false
            });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            sut.Handle(CreateCommand(), CancellationToken.None));

        await AssertNoPersistenceAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MR_T09_creates_only_medical_record_when_vaccinations_null_or_empty(bool useEmptyList)
    {
        ArrangeOwnedAppointment();
        ArrangeActiveDiagnostic();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        IReadOnlyCollection<CreateAppointmentMedicalRecordVaccinationItem>? vaccinations =
            useEmptyList ? Array.Empty<CreateAppointmentMedicalRecordVaccinationItem>() : null;

        var result = await sut.Handle(CreateCommand(vaccinations), CancellationToken.None);

        Assert.Empty(result.VaccinationIds);
        await vaccinationsRepository.DidNotReceive()
            .AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await medicalRecordsRepository.Received(1)
            .AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MR_T10_rejects_invalid_vaccination_before_persistence()
    {
        var applicationDate = DateTime.UtcNow.Date;
        var command = CreateCommand(
        [
            new CreateAppointmentMedicalRecordVaccinationItem(
                string.Empty,
                0,
                applicationDate,
                applicationDate.AddDays(-1))
        ]);

        var result = await validator.TestValidateAsync(command);

        Assert.False(result.IsValid);
        await medicalRecordsRepository.DidNotReceive()
            .AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>());
        await vaccinationsRepository.DidNotReceive()
            .AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MR_T11_propagates_cancellation_token_to_repositories_and_save()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        ArrangeOwnedAppointment();
        ArrangeActiveDiagnostic();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, token)
            .Returns(false);

        var applicationDate = DateTime.UtcNow.Date;
        await sut.Handle(
            CreateCommand(
            [
                new CreateAppointmentMedicalRecordVaccinationItem("Rabia", 1, applicationDate, null)
            ]),
            token);

        await appointmentsRepository.Received(1).GetByIdAsync(AppointmentId, token);
        await medicalRecordsRepository.Received(1).ExistsByAppointmentIdAsync(AppointmentId, token);
        await diagnosticsRepository.Received(1).GetByIdAsync(DiagnosticId, token);
        await medicalRecordsRepository.Received(1).AddAsync(Arg.Any<MedicalRecord>(), token);
        await vaccinationsRepository.Received(1).AddAsync(Arg.Any<Vaccination>(), token);
        await unitOfWork.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task MR_T12_does_not_use_GetAllAsync_as_fallback()
    {
        ArrangeOwnedAppointment();
        ArrangeActiveDiagnostic();
        medicalRecordsRepository.ExistsByAppointmentIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        await sut.Handle(CreateCommand(), CancellationToken.None);

        await medicalRecordsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await vaccinationsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await appointmentsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    private void ArrangeOwnedAppointment()
    {
        ArrangeAppointment(OwnVeterinarianId);
        ArrangeOwnedVeterinarian();
    }

    private void ArrangeAppointment(Guid veterinarianId)
    {
        var appointment = WithId(
            new Appointment(
                ClientPetId,
                veterinarianId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null),
            AppointmentId);

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
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

    private void ArrangeActiveDiagnostic()
    {
        diagnosticsRepository.GetByIdAsync(DiagnosticId, Arg.Any<CancellationToken>())
            .Returns(new Diagnostic
            {
                Id = DiagnosticId,
                Code = "DX-01",
                Name = "Activo",
                IsActive = true
            });
    }

    private static CreateAppointmentMedicalRecordCommand CreateCommand(
        IReadOnlyCollection<CreateAppointmentMedicalRecordVaccinationItem>? vaccinations = null,
        bool enforce = true)
    {
        return new CreateAppointmentMedicalRecordCommand(
            AppointmentId,
            DiagnosticId,
            "Síntomas",
            "Tratamiento",
            10m,
            38.5m,
            vaccinations,
            ActorUserAccountId,
            enforce);
    }

    private async Task AssertNoPersistenceAsync()
    {
        await medicalRecordsRepository.DidNotReceive()
            .AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>());
        await vaccinationsRepository.DidNotReceive()
            .AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static TEntity WithId<TEntity, TId>(TEntity entity, TId id)
        where TEntity : BaseEntity<TId>
    {
        typeof(BaseEntity<TId>).GetProperty(nameof(BaseEntity<TId>.Id))!.SetValue(entity, id);
        return entity;
    }
}
