using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Medications.Abstraction;
using Application.Medications.UseCases;
using Domain.Medications.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Medications;

public sealed class MedicationUseCasesTests
{
    [Fact]
    public async Task Create_rejects_a_code_that_already_exists()
    {
        var uow = CreateUnitOfWork(out var repository);
        repository.ExistsByCodeAsync(" med-001 ", Arg.Any<CancellationToken>(), null).Returns(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            new CreateMedicationCommandHandler(uow).Handle(
                new CreateMedicationCommand("Otro medicamento", " med-001 "),
                CancellationToken.None));

        Assert.Equal("Medications.CodeAlreadyExists", exception.Code);
        Assert.Contains("MED-001", exception.Message);
        await repository.DidNotReceive().AddAsync(Arg.Any<Medication>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_rejects_a_code_used_by_another_medication()
    {
        var uow = CreateUnitOfWork(out var repository);
        var current = new Medication("Actual", "MED-002");
        repository.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
        repository.ExistsByCodeAsync("med-001", Arg.Any<CancellationToken>(), current.Id).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new UpdateMedicationCommandHandler(uow).Handle(
                new UpdateMedicationCommand(current.Id, "Actual", "med-001", true),
                CancellationToken.None));

        Assert.Equal("MED-002", current.Code);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Medication>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_allows_the_current_medication_to_keep_its_code()
    {
        var uow = CreateUnitOfWork(out var repository);
        var current = new Medication("Actual", "MED-001");
        repository.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
        repository.ExistsByCodeAsync("med-001", Arg.Any<CancellationToken>(), current.Id).Returns(false);

        await new UpdateMedicationCommandHandler(uow).Handle(
            new UpdateMedicationCommand(current.Id, "Actual actualizado", " med-001 ", true),
            CancellationToken.None);

        Assert.Equal("MED-001", current.Code);
        await repository.Received(1).UpdateAsync(current, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Medication_normalizes_codes_and_keeps_empty_codes_null()
    {
        var medication = new Medication("Medicamento", " med-001 ");
        var withoutCode = new Medication("Sin codigo", "   ");

        Assert.Equal("MED-001", medication.Code);
        Assert.Null(withoutCode.Code);
    }

    private static IUnitOfWork CreateUnitOfWork(out IMedicationRepository repository)
    {
        var uow = Substitute.For<IUnitOfWork>();
        repository = Substitute.For<IMedicationRepository>();
        uow.MedicationsRepository.Returns(repository);
        return uow;
    }
}
