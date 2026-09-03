using Application.Common.Abstractions;
using Application.Veterinarians.Abstraction;
using Application.Veterinarians.UseCases;
using NSubstitute;
using Xunit;

namespace Application.Tests.Veterinarians;

public sealed class UpdateVeterinarianCommandValidatorTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly UpdateVeterinarianCommandValidator validator;

    public UpdateVeterinarianCommandValidatorTests()
    {
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        validator = new UpdateVeterinarianCommandValidator(unitOfWork);
    }

    [Fact]
    public async Task Validate_rejects_a_user_that_already_has_another_veterinarian_profile()
    {
        var command = Valid();
        veterinariansRepository.ExistsByUserIdAsync(command.UserId, Arg.Any<CancellationToken>(), command.Id)
            .Returns(true);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(UpdateVeterinarianCommand.UserId));
    }

    [Fact]
    public async Task Validate_excludes_its_own_id_when_checking_for_a_duplicate_user()
    {
        var command = Valid();
        veterinariansRepository.ExistsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        veterinariansRepository.ExistsByLicenseNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        await veterinariansRepository.Received(1).ExistsByUserIdAsync(command.UserId, Arg.Any<CancellationToken>(), command.Id);
    }

    private static UpdateVeterinarianCommand Valid() => new(
        Id: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        SpecialtyId: Guid.NewGuid(),
        LicenseNumber: "LIC-0001");
}
