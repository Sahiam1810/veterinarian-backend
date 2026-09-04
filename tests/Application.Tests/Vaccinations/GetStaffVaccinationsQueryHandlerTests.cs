using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Vaccinations.Abstraction;
using Application.Vaccinations.UseCases;
using Domain.Vaccinations.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vaccinations;

public sealed class GetStaffVaccinationsQueryHandlerTests
{
    private readonly IVaccinationRepository vaccinationsRepository = Substitute.For<IVaccinationRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetStaffVaccinationsQueryHandlerTests()
    {
        unitOfWork.VaccinationsRepository.Returns(vaccinationsRepository);
    }

    [Fact]
    public async Task GetAll_returns_the_staff_wide_collection_without_identity_inference()
    {
        var expected = new[] { CreateVaccination() };
        vaccinationsRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expected);
        var sut = new GetAllVaccinationsQueryHandler(unitOfWork);

        var result = await sut.Handle(new GetAllVaccinationsQuery(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetById_returns_the_requested_record_without_identity_inference()
    {
        var expected = CreateVaccination();
        vaccinationsRepository.GetByIdAsync(expected.Id, Arg.Any<CancellationToken>())
            .Returns(expected);
        var sut = new GetVaccinationByIdQueryHandler(unitOfWork);

        var result = await sut.Handle(
            new GetVaccinationByIdQuery(expected.Id),
            CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetById_throws_when_record_does_not_exist()
    {
        var vaccinationId = Guid.NewGuid();
        vaccinationsRepository.GetByIdAsync(vaccinationId, Arg.Any<CancellationToken>())
            .Returns((Vaccination?)null);
        var sut = new GetVaccinationByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetVaccinationByIdQuery(vaccinationId), CancellationToken.None));
    }

    private static Vaccination CreateVaccination() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Rabia",
        1,
        DateTime.UtcNow,
        DateTime.UtcNow.AddYears(1));
}
