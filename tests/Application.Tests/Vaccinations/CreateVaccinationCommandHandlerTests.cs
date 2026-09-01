using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.MedicalRecords.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Vaccinations.UseCases;
using Domain.MedicalRecords.Entities;
using Domain.Vaccinations.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vaccinations;

public sealed class CreateVaccinationCommandHandlerTests
{
    private static readonly Guid ClientPetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherClientPetId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid MedicalRecordId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IVaccinationRepository vaccinationsRepository = Substitute.For<IVaccinationRepository>();
    private readonly IMedicalRecordRepository medicalRecordRepository = Substitute.For<IMedicalRecordRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateVaccinationCommandHandler sut;

    public CreateVaccinationCommandHandlerTests()
    {
        unitOfWork.VaccinationsRepository.Returns(vaccinationsRepository);
        unitOfWork.MedicalRecordsRepository.Returns(medicalRecordRepository);
        sut = new CreateVaccinationCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_VAC_01_T01_creates_and_persists_vaccination_when_medical_record_exists_and_matches_client_pet()
    {
        // Arrange
        var medicalRecord = new MedicalRecord(
            ClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Síntomas",
            "Tratamiento",
            10.5m,
            38.5m);

        medicalRecordRepository
            .GetByIdAsync(MedicalRecordId, Arg.Any<CancellationToken>())
            .Returns(medicalRecord);

        var command = new CreateVaccinationCommand(
            ClientPetId,
            MedicalRecordId,
            "Rabia",
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1));

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        await vaccinationsRepository.Received(1).AddAsync(
            Arg.Is<Vaccination>(v => v.ClientPetId == ClientPetId && v.RecordId == MedicalRecordId && v.VaccineName == "Rabia"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VAC_01_T02_throws_NotFoundException_when_medical_record_does_not_exist()
    {
        // Arrange
        medicalRecordRepository
            .GetByIdAsync(MedicalRecordId, Arg.Any<CancellationToken>())
            .Returns((MedicalRecord?)null);

        var command = new CreateVaccinationCommand(
            ClientPetId,
            MedicalRecordId,
            "Rabia",
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await vaccinationsRepository.DidNotReceive().AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VAC_01_T03_throws_BadRequestException_with_exact_message_when_medical_record_belongs_to_different_client_pet()
    {
        // Arrange
        var medicalRecordForOtherPet = new MedicalRecord(
            OtherClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Síntomas",
            "Tratamiento",
            5.0m,
            39.0m);

        medicalRecordRepository
            .GetByIdAsync(MedicalRecordId, Arg.Any<CancellationToken>())
            .Returns(medicalRecordForOtherPet);

        var command = new CreateVaccinationCommand(
            ClientPetId,
            MedicalRecordId,
            "Rabia",
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
        Assert.Equal("La historia clínica no corresponde a la relación cliente-mascota indicada.", ex.Message);
        await vaccinationsRepository.DidNotReceive().AddAsync(Arg.Any<Vaccination>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VAC_01_T04_does_not_modify_or_persist_entities_when_coherence_validation_fails()
    {
        // Arrange
        var mismatchedRecord = new MedicalRecord(
            OtherClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Síntomas",
            "Tratamiento",
            12.0m,
            38.0m);

        medicalRecordRepository
            .GetByIdAsync(MedicalRecordId, Arg.Any<CancellationToken>())
            .Returns(mismatchedRecord);

        var command = new CreateVaccinationCommand(
            ClientPetId,
            MedicalRecordId,
            "Triple Felina",
            2,
            DateTime.UtcNow,
            null);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));

        await vaccinationsRepository.DidNotReceiveWithAnyArgs().AddAsync(null!, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_VAC_01_T05_propagates_cancellation_token()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var medicalRecord = new MedicalRecord(
            ClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Síntomas",
            "Tratamiento",
            8.0m,
            38.2m);

        medicalRecordRepository
            .GetByIdAsync(MedicalRecordId, cancellationToken)
            .Returns(medicalRecord);

        var command = new CreateVaccinationCommand(
            ClientPetId,
            MedicalRecordId,
            "Parvovirus",
            1,
            DateTime.UtcNow,
            null);

        // Act
        await sut.Handle(command, cancellationToken);

        // Assert
        await medicalRecordRepository.Received(1).GetByIdAsync(MedicalRecordId, cancellationToken);
        await vaccinationsRepository.Received(1).AddAsync(Arg.Any<Vaccination>(), cancellationToken);
        await unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }
}
