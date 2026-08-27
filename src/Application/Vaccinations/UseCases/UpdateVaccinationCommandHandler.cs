using Application.Common.Abstractions;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class UpdateVaccinationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVaccinationCommand, bool>
{
    public async Task<bool> Handle(
        UpdateVaccinationCommand request,
        CancellationToken cancellationToken)
    {
        var vaccination = await unitOfWork.VaccinationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (vaccination is null)
        {
            return false;
        }

        vaccination.Update(
            request.ClientPetId,
            request.RecordId,
            request.VaccineName,
            request.DoseNumber,
            request.ApplicationDate,
            request.NextDoseDate);

        await unitOfWork.VaccinationsRepository.UpdateAsync(
            vaccination,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
