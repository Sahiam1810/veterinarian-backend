using Application.Common.Abstractions;
using Application.Veterinarians.Abstraction;
using Application.Veterinarians.UseCases;
using NSubstitute;
using Xunit;

namespace Application.Tests.Veterinarians;

// P1 corregido: un mismo usuario podía terminar con dos perfiles de
// veterinario -- Veterinarians.UserId no tenía unique constraint ni chequeo
// de aplicación, dejando a /veterinarians/me (GetByUserIdAsync + FirstOrDefault)
// no determinístico si eso llegaba a pasar.
public sealed class CreateVeterinarianCommandValidatorTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IVeterinarianRepository veterinariansRepository = Substitute.For<IVeterinarianRepository>();
    private readonly CreateVeterinarianCommandValidator validator;

    public CreateVeterinarianCommandValidatorTests()
    {
        unitOfWork.VeterinariansRepository.Returns(veterinariansRepository);
        validator = new CreateVeterinarianCommandValidator(unitOfWork);
    }

    [Fact]
    public async Task Validate_rejects_a_user_that_already_has_a_veterinarian_profile()
    {
        var userId = Guid.NewGuid();
        veterinariansRepository.ExistsByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await validator.ValidateAsync(Valid() with { UserId = userId });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateVeterinarianCommand.UserId));
    }

    [Fact]
    public async Task Validate_accepts_a_user_without_an_existing_veterinarian_profile()
    {
        veterinariansRepository.ExistsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        veterinariansRepository.ExistsByLicenseNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        var result = await validator.ValidateAsync(Valid());

        Assert.True(result.IsValid);
    }

    private static CreateVeterinarianCommand Valid() => new(
        UserId: Guid.NewGuid(),
        SpecialtyId: Guid.NewGuid(),
        LicenseNumber: "LIC-0001");
}
