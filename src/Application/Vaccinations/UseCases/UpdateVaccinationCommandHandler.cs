using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class UpdateVaccinationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVaccinationCommand>
{
    public async Task Handle(
        UpdateVaccinationCommand request,
        CancellationToken cancellationToken)
    {
        var vaccination = await unitOfWork.VaccinationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Registro de vacunación no encontrado.");

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
    }
}
