using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Diagnostics.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.MedicalRecords.UseCases;
using Application.UserAccounts.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.Diagnostics.Entities;
using Domain.UserAccounts.Entities;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.MedicalRecords;

public sealed class CreateMedicalRecordCommandHandlerTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ClientPetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AppointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid VeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OtherVeterinarianId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DiagnosticId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly IDiagnosticRepository diagnosticsRepository = Substitute.For<IDiagnosticRepository>();
    private readonly IMedicalRecordRepository medicalRecordsRepository = Substitute.For<IMedicalRecordRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateMedicalRecordCommandHandler sut;

    public CreateMedicalRecordCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        unitOfWork.DiagnosticsRepository.Returns(diagnosticsRepository);
        unitOfWork.MedicalRecordsRepository.Returns(medicalRecordsRepository);
        sut = new CreateMedicalRecordCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_user_account_does_not_exist()
    {
        var appointment = CreateAppointment(ClientPetId, VeterinarianId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        var command = CreateCommand();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await medicalRecordsRepository.DidNotReceive().AddAsync(Arg.Any<Domain.MedicalRecords.Entities.MedicalRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_UnauthorizedException_when_veterinarian_is_not_assigned_to_appointment()
    {
        var appointment = CreateAppointment(ClientPetId, OtherVeterinarianId);
        var account = new UserAccountEntity(UserId, "vet", "vet@test.com", "Active");
        var veterinarian = new Veterinarian(UserId, Guid.NewGuid(), "LIC-001");
        var diagnostic = new Diagnostic
        {
            Id = DiagnosticId,
            Code = "DX-01",
            Name = "Prueba"
        };

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        veterinariansRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);
        diagnosticsRepository.GetByIdAsync(DiagnosticId, Arg.Any<CancellationToken>())
            .Returns(diagnostic);

        var command = CreateCommand();

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.Handle(command, CancellationToken.None));
        await medicalRecordsRepository.DidNotReceive().AddAsync(Arg.Any<Domain.MedicalRecords.Entities.MedicalRecord>(), Arg.Any<CancellationToken>());
    }

    private static Appointment CreateAppointment(Guid clientPetId, Guid veterinarianId)
    {
        return new Appointment(
            clientPetId,
            veterinarianId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);
    }

    private static CreateMedicalRecordCommand CreateCommand()
    {
        return new CreateMedicalRecordCommand(
            ClientPetId,
            AppointmentId,
            DiagnosticId,
            "Síntomas",
            "Tratamiento",
            10m,
            38.5m,
            UserAccountId);
    }
}
