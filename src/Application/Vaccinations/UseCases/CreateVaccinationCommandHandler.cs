using Application.Common.Abstractions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class CreateVaccinationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVaccinationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateVaccinationCommand request,
        CancellationToken cancellationToken)
    {
        var vaccination = new Vaccination(
            request.ClientPetId,
            request.RecordId,
            request.VaccineName,
            request.DoseNumber,
            request.ApplicationDate,
            request.NextDoseDate);

        await unitOfWork.VaccinationsRepository.AddAsync(
            vaccination,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return vaccination.Id;
    }
}
